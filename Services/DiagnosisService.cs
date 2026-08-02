using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class DiagnosisService : IDiagnosisService
    {
        private readonly IDiagnosisRepository _repo;
        public DiagnosisService(IDiagnosisRepository repo) => _repo = repo;

        public async Task<DiagnosisResponseDTO> Create(DiagnosisCreateDTO dto)
        {
            var entity = new Diagnosis
            {
                PatientId = dto.PatientId,
                DoctorId = (dto.DoctorId.HasValue && dto.DoctorId.Value > 0) ? dto.DoctorId.Value : null,
                Symptoms = dto.Symptoms,
                ClinicalExamination = dto.ClinicalExamination,
                DiagnosisResult = dto.DiagnosisResult,
                Note = dto.Note,
                DiagnosisDate = dto.DiagnosisDate == default ? DateTime.Now : dto.DiagnosisDate,
                CreatedAt = DateTime.Now
            };
            var created = await _repo.Create(entity);
            var full = await _repo.GetById(created.Id);
            return Map(full);
        }

        public async Task<DiagnosisResponseDTO> GetById(int id)
        {
            var entity = await _repo.GetById(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<List<DiagnosisResponseDTO>> GetByPatient(int patientId)
        {
            var list = await _repo.GetByPatient(patientId);
            return list.Select(Map).ToList();
        }

        public async Task<List<DiagnosisResponseDTO>> GetByDoctor(int doctorId)
        {
            var list = await _repo.GetByDoctor(doctorId);
            return list.Select(Map).ToList();
        }

        public async Task<List<DiagnosisResponseDTO>> GetAll()
        {
            var list = await _repo.GetAll();
            return list.Select(Map).ToList();
        }

        public async Task<DiagnosisResponseDTO> Update(int id, DiagnosisUpdateDTO dto)
        {
            var entity = await _repo.GetById(id);
            if (entity == null) return null;

            entity.Symptoms = dto.Symptoms ?? entity.Symptoms;
            entity.ClinicalExamination = dto.ClinicalExamination ?? entity.ClinicalExamination;
            entity.DiagnosisResult = dto.DiagnosisResult ?? entity.DiagnosisResult;
            entity.Note = dto.Note ?? entity.Note;

            var updated = await _repo.Update(entity);
            return Map(updated);
        }

        public async Task<bool> Delete(int id) => await _repo.Delete(id);

        public async Task<List<SymptomQuestionDTO>> GetQuestionsAsync()
        {
            var questions = await _repo.GetQuestionsWithAnswersAsync();
            return questions.Select(q => new SymptomQuestionDTO
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                QuestionOrder = q.QuestionOrder,
                Category = q.Category,
                AnswerOptions = q.AnswerOptions.OrderBy(a => a.OptionOrder).Select(a => new AnswerOptionDTO
                {
                    Id = a.Id,
                    QuestionId = a.QuestionId,
                    OptionText = a.OptionText,
                    OptionOrder = a.OptionOrder
                }).ToList()
            }).ToList();
        }

        public async Task<DiagnosisResultDTO> ClassifyAsync(DiagnosisClassifyRequestDTO dto, int? currentUserId)
        {
            const int MIN_THRESHOLD = 5;
            var syndromes = await _repo.GetSyndromeTypesAsync();
            var mappings = await _repo.GetScoreMappingsAsync();

            var scores = syndromes.ToDictionary(s => s.Code, s => 0);
            var submittedOptionIds = dto.Answers.Select(a => a.AnswerOptionId).ToHashSet();

            foreach (var map in mappings)
            {
                if (submittedOptionIds.Contains(map.AnswerOptionId))
                {
                    var syn = syndromes.FirstOrDefault(s => s.Id == map.SyndromeTypeId);
                    if (syn != null && scores.ContainsKey(syn.Code))
                    {
                        scores[syn.Code] += map.Points;
                    }
                }
            }

            var sortedScores = scores.OrderByDescending(kv => kv.Value).ToList();
            var topCode = sortedScores.Count > 0 ? sortedScores[0].Key : null;
            var topScore = sortedScores.Count > 0 ? sortedScores[0].Value : 0;
            var secondCode = sortedScores.Count > 1 ? sortedScores[1].Key : null;
            var secondScore = sortedScores.Count > 1 ? sortedScores[1].Value : 0;

            SyndromeType primarySyn = null;
            SyndromeType secondarySyn = null;

            if (topScore < MIN_THRESHOLD)
            {
                primarySyn = new SyndromeType
                {
                    Id = 0,
                    Code = "UNCLEAR",
                    Name = "Kết quả chưa đủ rõ ràng",
                    Description = "Dựa trên các triệu chứng bạn đã chọn, không có thể bệnh nào đạt ngưỡng tối thiểu để kết luận chắc chắn.",
                    RecommendationText = "Khuyên bạn nên sắp xếp lịch khám trực tiếp với bác sĩ Đông y để được bắt mạch và chẩn đoán chi tiết hơn."
                };
            }
            else
            {
                primarySyn = syndromes.FirstOrDefault(s => s.Code == topCode);
                if (secondScore > 0 && (topScore - secondScore) < (0.2 * topScore))
                {
                    secondarySyn = syndromes.FirstOrDefault(s => s.Code == secondCode);
                }
            }

            string desc = primarySyn.Description;
            if (secondarySyn != null)
            {
                desc += $" Kết hợp triệu chứng phụ của thể: {secondarySyn.Name} ({secondarySyn.Description}).";
            }

            string recText = primarySyn.RecommendationText;
            if (secondarySyn != null && !string.IsNullOrEmpty(secondarySyn.RecommendationText))
            {
                recText += $" Gợi ý thêm cho thể {secondarySyn.Name}: {secondarySyn.RecommendationText}";
            }

            var resultDto = new DiagnosisResultDTO
            {
                PrimarySyndrome = new SyndromeTypeDTO
                {
                    Id = primarySyn.Id,
                    Code = primarySyn.Code,
                    Name = primarySyn.Name,
                    Description = primarySyn.Description,
                    RecommendationText = primarySyn.RecommendationText
                },
                SecondarySyndrome = secondarySyn == null ? null : new SyndromeTypeDTO
                {
                    Id = secondarySyn.Id,
                    Code = secondarySyn.Code,
                    Name = secondarySyn.Name,
                    Description = secondarySyn.Description,
                    RecommendationText = secondarySyn.RecommendationText
                },
                Scores = scores,
                Description = desc,
                RecommendationText = recText,
                SuggestedHerbalMedicineIds = new List<int>()
            };

            if (currentUserId.HasValue && currentUserId.Value > 0)
            {
                var diagEntity = new Diagnosis
                {
                    PatientId = currentUserId.Value,
                    DoctorId = null,
                    PrimarySyndromeId = primarySyn.Id > 0 ? primarySyn.Id : null,
                    SecondarySyndromeId = secondarySyn?.Id,
                    ScoreSnapshotJson = JsonSerializer.Serialize(scores),
                    Symptoms = "Tự chẩn đoán triệu chứng theo bảng câu hỏi Đông Y",
                    ClinicalExamination = "Biện chứng luận trị theo mô hình tính điểm triệu chứng",
                    DiagnosisResult = secondarySyn == null 
                        ? $"Thể bệnh: {primarySyn.Name}" 
                        : $"Thể bệnh chính: {primarySyn.Name} (Kết hợp {secondarySyn.Name})",
                    Note = recText,
                    DiagnosisDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    DiagnosisAnswers = dto.Answers.Select(a => new DiagnosisAnswer
                    {
                        QuestionId = a.QuestionId,
                        AnswerOptionId = a.AnswerOptionId
                    }).ToList()
                };

                await _repo.Create(diagEntity);
            }

            return resultDto;
        }

        private DiagnosisResponseDTO Map(Diagnosis d)
        {
            return new DiagnosisResponseDTO
            {
                Id = d.Id,
                PatientId = d.PatientId,
                PatientName = d.Patient?.UserName,
                DoctorId = d.DoctorId,
                DoctorName = d.Doctor?.UserName,
                Symptoms = d.Symptoms,
                ClinicalExamination = d.ClinicalExamination,
                DiagnosisResult = d.DiagnosisResult,
                Note = d.Note,
                DiagnosisDate = d.DiagnosisDate,
                CreatedAt = d.CreatedAt,
                PrescriptionCount = d.Prescriptions?.Count ?? 0
            };
        }
    }
}

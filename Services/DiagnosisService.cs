using BusinessObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class DiagnosisService : IDiagnosisService
    {
        private readonly IDiagnosisRepository _repo;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiagnosisService> _logger;

        private const int MIN_QUESTIONS_BEFORE_STOP = 5;
        private static readonly string[] ModelsToTry = { "gemini-2.5-flash", "gemini-3.5-flash-lite", "gemini-2.0-flash", "gemini-flash-latest" };

        public DiagnosisService(IDiagnosisRepository repo, HttpClient httpClient, IConfiguration configuration, ILogger<DiagnosisService> logger)
        {
            _repo = repo;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

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

            if (primarySyn.Id > 0)
            {
                resultDto.SuggestedMedicines = await SuggestMedicinesAsync(primarySyn, secondarySyn);
                resultDto.SuggestedHerbalMedicineIds = resultDto.SuggestedMedicines.Select(m => m.MedicineId).ToList();
            }

            return resultDto;
        }

        // Câu hỏi tự chẩn đoán thích ứng: thay vì hỏi đủ toàn bộ bộ câu hỏi cố định theo thứ tự,
        // mỗi lần chọn câu hỏi CHƯA trả lời có khả năng phân biệt tốt nhất giữa các thể bệnh đang
        // dẫn đầu hiện tại (dựa trên độ chênh lệch điểm số các phương án trả lời của câu hỏi đó
        // đối với top 3 thể bệnh dẫn đầu) — giúp rút ngắn bảng hỏi khi kết quả đã đủ rõ ràng sớm.
        public async Task<NextQuestionResponseDTO> GetNextQuestionAsync(List<AnswerSubmissionDTO> answeredSoFar)
        {
            var questions = await _repo.GetQuestionsWithAnswersAsync();
            var syndromes = await _repo.GetSyndromeTypesAsync();
            var mappings = await _repo.GetScoreMappingsAsync();

            answeredSoFar ??= new List<AnswerSubmissionDTO>();
            var answeredQuestionIds = answeredSoFar.Select(a => a.QuestionId).ToHashSet();
            var remaining = questions.Where(q => !answeredQuestionIds.Contains(q.Id)).OrderBy(q => q.QuestionOrder).ToList();

            const int MIN_THRESHOLD = 5;

            // Điểm số tạm tính từ các câu đã trả lời, dùng để xác định thể bệnh đang dẫn đầu.
            var submittedOptionIds = answeredSoFar.Select(a => a.AnswerOptionId).ToHashSet();
            var scores = syndromes.ToDictionary(s => s.Code, s => 0);
            var syndromeCodeByOptionId = new Dictionary<int, List<(string code, int points)>>();
            foreach (var map in mappings)
            {
                var syn = syndromes.FirstOrDefault(s => s.Id == map.SyndromeTypeId);
                if (syn == null) continue;
                if (!syndromeCodeByOptionId.TryGetValue(map.AnswerOptionId, out var list))
                {
                    list = new List<(string, int)>();
                    syndromeCodeByOptionId[map.AnswerOptionId] = list;
                }
                list.Add((syn.Code, map.Points));

                if (submittedOptionIds.Contains(map.AnswerOptionId))
                {
                    scores[syn.Code] += map.Points;
                }
            }

            if (remaining.Count == 0)
            {
                return new NextQuestionResponseDTO { NextQuestion = null, Done = true, AnsweredCount = answeredSoFar.Count, MinRecommended = MIN_QUESTIONS_BEFORE_STOP };
            }

            if (answeredSoFar.Count >= MIN_QUESTIONS_BEFORE_STOP)
            {
                var sorted = scores.OrderByDescending(kv => kv.Value).ToList();
                var topScore = sorted.Count > 0 ? sorted[0].Value : 0;
                var secondScore = sorted.Count > 1 ? sorted[1].Value : 0;
                var decisive = topScore >= MIN_THRESHOLD && (secondScore == 0 || (topScore - secondScore) >= 0.2 * topScore);
                if (decisive)
                {
                    return new NextQuestionResponseDTO { NextQuestion = null, Done = true, AnsweredCount = answeredSoFar.Count, MinRecommended = MIN_QUESTIONS_BEFORE_STOP };
                }
            }

            // 3 thể bệnh đang dẫn đầu (early game điểm bằng 0 hết -> coi như tất cả đều "đang dẫn đầu",
            // chọn câu hỏi tổng quát nhất theo QuestionOrder gốc).
            var leadingCodes = scores.OrderByDescending(kv => kv.Value).Take(3).Select(kv => kv.Key).ToHashSet();

            SymptomQuestion bestQuestion = remaining[0];
            double bestImpact = -1;
            foreach (var q in remaining)
            {
                var optionSums = q.AnswerOptions.Select(o =>
                    syndromeCodeByOptionId.TryGetValue(o.Id, out var list)
                        ? list.Where(x => leadingCodes.Contains(x.code)).Sum(x => x.points)
                        : 0
                ).ToList();

                double impact = optionSums.Count > 0 ? optionSums.Max() - optionSums.Min() : 0;
                if (impact > bestImpact)
                {
                    bestImpact = impact;
                    bestQuestion = q;
                }
            }

            return new NextQuestionResponseDTO
            {
                NextQuestion = new SymptomQuestionDTO
                {
                    Id = bestQuestion.Id,
                    QuestionText = bestQuestion.QuestionText,
                    QuestionOrder = bestQuestion.QuestionOrder,
                    Category = bestQuestion.Category,
                    AnswerOptions = bestQuestion.AnswerOptions.OrderBy(a => a.OptionOrder).Select(a => new AnswerOptionDTO
                    {
                        Id = a.Id,
                        QuestionId = a.QuestionId,
                        OptionText = a.OptionText,
                        OptionOrder = a.OptionOrder
                    }).ToList()
                },
                Done = false,
                AnsweredCount = answeredSoFar.Count,
                MinRecommended = MIN_QUESTIONS_BEFORE_STOP
            };
        }

        // Gợi ý dược liệu phù hợp thể bệnh vừa chẩn đoán — ưu tiên dùng AI để đọc Tính vị/Công dụng
        // thật của từng sản phẩm và giải thích lý do, rơi về so khớp từ khóa đơn giản nếu AI lỗi/không
        // có API key (vẫn hoạt động được, chỉ không có phần lý do do AI viết).
        private async Task<List<SuggestedMedicineDTO>> SuggestMedicinesAsync(SyndromeType primary, SyndromeType secondary)
        {
            var candidates = await _repo.GetHerbalCandidatesAsync(60);
            if (candidates.Count == 0) return new List<SuggestedMedicineDTO>();

            var apiKey = _configuration["Gemini:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                var aiResult = await TrySuggestWithAI(candidates, primary, secondary, apiKey);
                if (aiResult != null && aiResult.Count > 0) return aiResult;
            }

            return SuggestByKeywordFallback(candidates, primary);
        }

        private async Task<List<SuggestedMedicineDTO>?> TrySuggestWithAI(List<HerbalMedicineInfo> candidates, SyndromeType primary, SyndromeType secondary, string apiKey)
        {
            try
            {
                var candidateLines = candidates.Select(c =>
                    $"- id={c.MedicineId}, tên=\"{c.Medicine!.Name}\", công dụng=\"{Truncate(c.Effects, 120)}\", tính vị=\"{Truncate(c.Properties, 80)}\"");
                var candidateBlock = string.Join("\n", candidateLines);

                var syndromeText = secondary == null
                    ? $"{primary.Name}: {primary.Description}"
                    : $"{primary.Name}: {primary.Description}\nKết hợp thể phụ {secondary.Name}: {secondary.Description}";

                string prompt = $@"Bạn là Dược sĩ tư vấn Đông Y. Người dùng vừa được phân loại thể bệnh sau:
{syndromeText}

Danh sách dược liệu đang có sẵn để bán (CHỈ được chọn id có trong danh sách này, không tự tạo id khác):
{candidateBlock}

Hãy chọn tối đa 5 sản phẩm phù hợp nhất với thể bệnh trên, giải thích ngắn gọn 1 câu vì sao mỗi sản phẩm phù hợp (dựa trên công dụng/tính vị đã cho). Trả lời CHỈ bằng JSON, không thêm giải thích ngoài:
{{ ""suggestions"": [ {{ ""medicineId"": 0, ""reason"": ""...""}} ] }}";

                var payload = new
                {
                    contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                    generationConfig = new { responseMimeType = "application/json" }
                };
                var body = JsonSerializer.Serialize(payload);

                HttpResponseMessage? response = null;
                string responseString = "";
                foreach (var model in ModelsToTry)
                {
                    try
                    {
                        var req = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
                        {
                            Content = new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json")
                        };
                        req.Headers.Add("x-goog-api-key", apiKey);
                        var res = await _httpClient.SendAsync(req);
                        var resBody = await res.Content.ReadAsStringAsync();
                        if (res.IsSuccessStatusCode) { response = res; responseString = resBody; break; }
                        else if (response == null) { response = res; responseString = resBody; }
                    }
                    catch (Exception modelEx)
                    {
                        if (response == null) responseString = modelEx.Message;
                    }
                }

                if (response == null || !response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Diagnosis medicine-suggestion Gemini error: {Body}", responseString.Length > 300 ? responseString.Substring(0, 300) : responseString);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;
                if (!root.TryGetProperty("candidates", out var cands) || cands.GetArrayLength() == 0) return null;
                if (!cands[0].TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0 ||
                    !parts[0].TryGetProperty("text", out var textProp)) return null;

                var aiJsonText = textProp.GetString();
                if (string.IsNullOrEmpty(aiJsonText)) return null;

                using var aiDoc = JsonDocument.Parse(aiJsonText);
                if (!aiDoc.RootElement.TryGetProperty("suggestions", out var sugArr) || sugArr.ValueKind != JsonValueKind.Array) return null;

                var result = new List<SuggestedMedicineDTO>();
                foreach (var s in sugArr.EnumerateArray())
                {
                    if (!s.TryGetProperty("medicineId", out var idProp) || !idProp.TryGetInt32(out var mid)) continue;
                    var match = candidates.FirstOrDefault(c => c.MedicineId == mid);
                    if (match?.Medicine == null) continue; // AI chỉ được chọn trong danh sách ứng viên thật

                    result.Add(new SuggestedMedicineDTO
                    {
                        MedicineId = match.MedicineId!.Value,
                        Name = match.Medicine.Name,
                        Price = match.Medicine.Price,
                        ImageUrl = match.Medicine.ImageUrl,
                        Reason = s.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "",
                        IsAiGenerated = true
                    });
                    if (result.Count >= 5) break;
                }
                return result.Count > 0 ? result : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Diagnosis medicine-suggestion Gemini call failed");
                return null;
            }
        }

        private static List<SuggestedMedicineDTO> SuggestByKeywordFallback(List<HerbalMedicineInfo> candidates, SyndromeType primary)
        {
            var keywords = (primary.Name + " " + primary.Description)
                .Split(new[] { ' ', ',', '.', ';', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3)
                .Select(w => w.ToLowerInvariant())
                .Distinct()
                .ToList();

            return candidates
                .Select(c => new
                {
                    Info = c,
                    Score = keywords.Count(k => (c.Effects ?? "").ToLowerInvariant().Contains(k) || (c.Properties ?? "").ToLowerInvariant().Contains(k))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => new SuggestedMedicineDTO
                {
                    MedicineId = x.Info.MedicineId!.Value,
                    Name = x.Info.Medicine!.Name,
                    Price = x.Info.Medicine.Price,
                    ImageUrl = x.Info.Medicine.ImageUrl,
                    Reason = $"Công dụng liên quan đến thể {primary.Name} (gợi ý theo từ khóa, chưa qua AI).",
                    IsAiGenerated = false
                })
                .ToList();
        }

        private static string Truncate(string? s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > max ? s.Substring(0, max) + "..." : s);

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

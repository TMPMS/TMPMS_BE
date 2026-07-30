using System;
using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class DiagnosisCreateDTO
    {
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public string Symptoms { get; set; }
        public string ClinicalExamination { get; set; }
        public string DiagnosisResult { get; set; }
        public string Note { get; set; }
        public DateTime DiagnosisDate { get; set; }
    }

    public class DiagnosisUpdateDTO
    {
        public string Symptoms { get; set; }
        public string ClinicalExamination { get; set; }
        public string DiagnosisResult { get; set; }
        public string Note { get; set; }
    }

    public class DiagnosisResponseDTO
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int? DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string Symptoms { get; set; }
        public string ClinicalExamination { get; set; }
        public string DiagnosisResult { get; set; }
        public string Note { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PrescriptionCount { get; set; }
    }

    public class DiagnosisDTOs
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int? DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string Symptoms { get; set; }
        public string ClinicalExamination { get; set; }
        public string DiagnosisResult { get; set; }
        public string Note { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PrescriptionCount { get; set; }
    }

    public class AnswerOptionDTO
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int OptionOrder { get; set; }
    }

    public class SymptomQuestionDTO
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public string Category { get; set; }
        public List<AnswerOptionDTO> AnswerOptions { get; set; } = new List<AnswerOptionDTO>();
    }

    public class SyndromeTypeDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; }
        public string RecommendationText { get; set; }
    }

    public class AnswerSubmissionDTO
    {
        public int QuestionId { get; set; }
        public int AnswerOptionId { get; set; }
    }

    public class DiagnosisClassifyRequestDTO
    {
        public List<AnswerSubmissionDTO> Answers { get; set; } = new List<AnswerSubmissionDTO>();
    }

    public class DiagnosisResultDTO
    {
        public SyndromeTypeDTO PrimarySyndrome { get; set; }
        public SyndromeTypeDTO SecondarySyndrome { get; set; }
        public Dictionary<string, int> Scores { get; set; } = new Dictionary<string, int>();
        public string Description { get; set; }
        public string RecommendationText { get; set; }
        public List<int> SuggestedHerbalMedicineIds { get; set; } = new List<int>();
    }
}

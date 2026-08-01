using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class HealthQuizListDTO
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
    }

    public class HealthQuizDetailDTO
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public List<QuizQuestionDTO> Questions { get; set; } = new List<QuizQuestionDTO>();
    }

    public class QuizQuestionDTO
    {
        public int Id { get; set; }
        public int QuestionOrder { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<QuizAnswerOptionDTO> AnswerOptions { get; set; } = new List<QuizAnswerOptionDTO>();
    }

    public class QuizAnswerOptionDTO
    {
        public int Id { get; set; }
        public int OptionOrder { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int Points { get; set; }
    }

    public class QuizAnswerItemDTO
    {
        public int QuestionId { get; set; }
        public int AnswerOptionId { get; set; }
    }

    public class QuizSubmitRequestDTO
    {
        public List<QuizAnswerItemDTO> Answers { get; set; } = new List<QuizAnswerItemDTO>();
    }

    public class QuizResultBandDTO
    {
        public string Label { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
        public string Description { get; set; } = string.Empty;
        public string RecommendationText { get; set; } = string.Empty;
    }

    public class QuizSubmitResponseDTO
    {
        public int TotalScore { get; set; }
        public QuizResultBandDTO ResultBand { get; set; } = new QuizResultBandDTO();
    }
}

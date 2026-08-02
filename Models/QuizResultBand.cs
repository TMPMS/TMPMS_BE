namespace TMPMS.Models
{
    public class QuizResultBand
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int MinScore { get; set; }
        public int MaxScore { get; set; }
        public string Label { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "Low"; // Low, Medium, High
        public string Description { get; set; } = string.Empty;
        public string RecommendationText { get; set; } = string.Empty;

        public HealthQuiz? Quiz { get; set; }
    }
}

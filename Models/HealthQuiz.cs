using System.Collections.Generic;

namespace TMPMS.Models
{
    public class HealthQuiz
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
        public ICollection<QuizResultBand> ResultBands { get; set; } = new List<QuizResultBand>();
    }
}

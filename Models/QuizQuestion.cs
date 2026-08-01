using System.Collections.Generic;

namespace TMPMS.Models
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }

        public HealthQuiz? Quiz { get; set; }
        public ICollection<QuizAnswerOption> AnswerOptions { get; set; } = new List<QuizAnswerOption>();
    }
}

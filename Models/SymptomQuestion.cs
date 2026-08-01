using System.Collections.Generic;

namespace BusinessObjects
{
    public class SymptomQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public string? Category { get; set; }

        public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
    }
}

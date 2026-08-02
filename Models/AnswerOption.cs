using System.Collections.Generic;

namespace BusinessObjects
{
    public class AnswerOption
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int OptionOrder { get; set; }

        public SymptomQuestion Question { get; set; }
        public ICollection<AnswerScoreMapping> ScoreMappings { get; set; } = new List<AnswerScoreMapping>();
    }
}

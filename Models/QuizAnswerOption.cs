namespace TMPMS.Models
{
    public class QuizAnswerOption
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int OptionOrder { get; set; }
        public int Points { get; set; }

        public QuizQuestion? Question { get; set; }
    }
}

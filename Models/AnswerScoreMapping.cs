namespace BusinessObjects
{
    public class AnswerScoreMapping
    {
        public int Id { get; set; }
        public int AnswerOptionId { get; set; }
        public int SyndromeTypeId { get; set; }
        public int Points { get; set; }

        public AnswerOption AnswerOption { get; set; }
        public SyndromeType SyndromeType { get; set; }
    }
}

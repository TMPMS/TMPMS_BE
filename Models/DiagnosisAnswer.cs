namespace BusinessObjects
{
    public class DiagnosisAnswer
    {
        public int Id { get; set; }
        public int DiagnosisId { get; set; }
        public int QuestionId { get; set; }
        public int AnswerOptionId { get; set; }

        public Diagnosis Diagnosis { get; set; }
        public SymptomQuestion Question { get; set; }
        public AnswerOption AnswerOption { get; set; }
    }
}

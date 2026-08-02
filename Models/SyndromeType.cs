namespace BusinessObjects
{
    public class SyndromeType
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RecommendationText { get; set; }
    }
}

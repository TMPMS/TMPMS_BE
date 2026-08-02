namespace TMPMS.DTOs
{
    public class YouTubeVideoDto
    {
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string PublishedAt { get; set; } = string.Empty;
        public long LikeCount { get; set; }
        public long ViewCount { get; set; }
        public string EmbedUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DurationFormatted { get; set; } = "02:30";
        public string ViewCountFormatted { get; set; } = "1.2K";
    }

    public class HealthReelsResponseDto
    {
        public List<YouTubeVideoDto> Videos { get; set; } = new List<YouTubeVideoDto>();
        public bool IsFallback { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }
}

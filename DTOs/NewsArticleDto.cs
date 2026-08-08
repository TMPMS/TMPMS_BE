using System;

namespace TMPMS.DTOs
{
    public class NewsArticleDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string image_url => ImageUrl;
        public DateTime PublishedDate { get; set; }
        public string published_date => PublishedDate.ToString("yyyy-MM-dd");
        public bool IsActive { get; set; } = true;
    }

    public class NewsArticleCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime? PublishedDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

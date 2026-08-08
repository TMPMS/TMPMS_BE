using System;

namespace BusinessObjects
{
    public class NewsArticle
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Excerpt { get; set; }

        public string Content { get; set; }

        public string Tag { get; set; }

        public string ImageUrl { get; set; }

        public DateTime PublishedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}

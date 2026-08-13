using System;

namespace TMPMS.DTOs
{
    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewCreateDto
    {
        public int UserId { get; set; }
        public int MedicineId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
    }
}

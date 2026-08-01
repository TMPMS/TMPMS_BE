using BusinessObjects;
using System;

namespace TMPMS.Models
{
    public class PharmacyChatMessage
    {
        public int Id { get; set; }

        public int SessionId { get; set; }
        public PharmacyChatSession? Session { get; set; }

        public int SenderId { get; set; }
        public User? Sender { get; set; }

        public string SenderRole { get; set; } = "User"; // "User", "Pharmacy", "Admin"

        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}

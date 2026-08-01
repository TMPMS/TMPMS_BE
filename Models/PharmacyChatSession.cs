using BusinessObjects;
using System;
using System.Collections.Generic;

namespace TMPMS.Models
{
    public class PharmacyChatSession
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? AssignedPharmacistId { get; set; }
        public User? AssignedPharmacist { get; set; }

        public string Status { get; set; } = "Open"; // Open, Assigned, Closed

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastMessageAt { get; set; } = DateTime.Now;

        public ICollection<PharmacyChatMessage> Messages { get; set; } = new List<PharmacyChatMessage>();
    }
}

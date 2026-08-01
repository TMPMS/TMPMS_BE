using System;
using BusinessObjects;

namespace TMPMS.Models
{
    public class QuizSession
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int? UserId { get; set; }
        public int TotalScore { get; set; }
        public int? ResultBandId { get; set; }
        public string? ScoreSnapshotJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public HealthQuiz? Quiz { get; set; }
        public User? User { get; set; }
        public QuizResultBand? ResultBand { get; set; }
    }
}

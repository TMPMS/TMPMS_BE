using System;
using System.Collections.Generic;

namespace BusinessObjects
{
    // Phiếu chẩn đoán của bác sĩ / Tự chẩn đoán Đông Y cho bệnh nhân
    public class Diagnosis
    {
        public int Id { get; set; }
        public int PatientId { get; set; }      // UserId bệnh nhân
        public int? DoctorId { get; set; }        // UserId bác sĩ (Role = Doctor, null nếu tự chẩn đoán)
        public string Symptoms { get; set; }             // Triệu chứng
        public string ClinicalExamination { get; set; }  // Khám lâm sàng
        public string DiagnosisResult { get; set; }      // Kết luận chẩn đoán
        public string Note { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? PrimarySyndromeId { get; set; }
        public int? SecondarySyndromeId { get; set; }
        public string? ScoreSnapshotJson { get; set; }

        public User Patient { get; set; }
        public User? Doctor { get; set; }
        public SyndromeType? PrimarySyndrome { get; set; }
        public SyndromeType? SecondarySyndrome { get; set; }
        public ICollection<Prescription> Prescriptions { get; set; }
        public ICollection<DiagnosisAnswer> DiagnosisAnswers { get; set; } = new List<DiagnosisAnswer>();
    }
}

using System;
using System.Collections.Generic;

namespace BusinessObjects
{
    // Phiếu chẩn đoán của bác sĩ cho bệnh nhân
    public class Diagnosis
    {
        public int Id { get; set; }
        public int PatientId { get; set; }      // UserId bệnh nhân
        public int DoctorId { get; set; }        // UserId bác sĩ (Role = Doctor)
        public string Symptoms { get; set; }             // Triệu chứng
        public string ClinicalExamination { get; set; }  // Khám lâm sàng
        public string DiagnosisResult { get; set; }      // Kết luận chẩn đoán
        public string Note { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public User Patient { get; set; }
        public User Doctor { get; set; }
        public ICollection<Prescription> Prescriptions { get; set; }
    }
}

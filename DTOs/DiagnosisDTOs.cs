using System;
using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class DiagnosisCreateDTO
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string Symptoms { get; set; }
        public string ClinicalExamination { get; set; }
        public string DiagnosisResult { get; set; }
        public string Note { get; set; }
        public DateTime DiagnosisDate { get; set; }
    }

    public class DiagnosisUpdateDTO
    {
        public string Symptoms { get; set; }
        public string ClinicalExamination { get; set; }
        public string DiagnosisResult { get; set; }
        public string Note { get; set; }
    }

    public class DiagnosisResponseDTO
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string Symptoms { get; set; }
        public string ClinicalExamination { get; set; }
        public string DiagnosisResult { get; set; }
        public string Note { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PrescriptionCount { get; set; }
    }

    public class DiagnosisDTOs
    {
        public int Id { get; set; }

        public int PatientId { get; set; }

        public string PatientName { get; set; }

        public int DoctorId { get; set; }

        public string DoctorName { get; set; }

        public string Symptoms { get; set; }

        public string ClinicalExamination { get; set; }

        public string DiagnosisResult { get; set; }

        public string Note { get; set; }

        public DateTime DiagnosisDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public int PrescriptionCount { get; set; }
    }
}

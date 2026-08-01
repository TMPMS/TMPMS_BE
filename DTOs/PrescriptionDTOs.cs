using System;
using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class PrescriptionItemCreateDTO
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
    }

    public class PrescriptionCreateDTO
    {
        public int UserId { get; set; }
        public int? DiagnosisId { get; set; }
        public int? DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string Hospital { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string ImageUrl { get; set; }
        public List<PrescriptionItemCreateDTO> Items { get; set; } = new();
    }

    public class PrescriptionStatusUpdateDTO
    {
        // Pending, Approved, Rejected, Fulfilled
        public string Status { get; set; }
        public string RejectReason { get; set; }
    }

    public class PrescriptionItemResponseDTO
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public bool RequiresPrescription { get; set; }
    }

    public class PrescriptionResponseDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? DiagnosisId { get; set; }
        public int? DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string Hospital { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        public List<PrescriptionItemResponseDTO> Items { get; set; } = new();
    }
}

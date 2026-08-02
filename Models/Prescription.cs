using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Prescription
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int? DiagnosisId { get; set; }
        public int? DoctorId { get; set; }
        public int? AppointmentId { get; set; }

        public string DoctorName { get; set; }

        public string Hospital { get; set; }

        public DateTime PrescriptionDate { get; set; }

        public string? DiagnosisNote { get; set; }

        public string ImageUrl { get; set; }

        public string Status { get; set; }

        public User User { get; set; }
        public User Doctor { get; set; }
        public Diagnosis Diagnosis { get; set; }
        public Appointment Appointment { get; set; }

        public ICollection<PrescriptionItem> PrescriptionItems { get; set; }
    }
}

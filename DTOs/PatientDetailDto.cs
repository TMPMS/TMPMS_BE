using BusinessObjects;

namespace TMPMS.DTOs
{
    public class PatientDetailDto
    {
        public PatientDto Patient { get; set; }

        public List<UserAddress> Addresses { get; set; }

        public List<Diagnosis> Diagnoses { get; set; }
    }
}

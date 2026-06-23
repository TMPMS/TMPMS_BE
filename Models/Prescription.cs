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

        public string DoctorName { get; set; }

        public string Hospital { get; set; }

        public DateTime PrescriptionDate { get; set; }

        public string ImageUrl { get; set; }

        public string Status { get; set; }

        public User User { get; set; }

        public ICollection<PrescriptionItem> PrescriptionItems { get; set; }
    }
}

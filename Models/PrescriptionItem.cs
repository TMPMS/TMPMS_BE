using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class PrescriptionItem
    {
        public int Id { get; set; }

        public int PrescriptionId { get; set; }

        public int MedicineId { get; set; }

        public int Quantity { get; set; }

        public Prescription Prescription { get; set; }

        public Medicine Medicine { get; set; }
    }
}

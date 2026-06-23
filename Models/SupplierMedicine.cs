using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class SupplierMedicine
    {
        public int SupplierId { get; set; }

        public int MedicineId { get; set; }

        public Supplier Supplier { get; set; }

        public Medicine Medicine { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Supplier
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }

        public string ContactPerson { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public string TaxCode { get; set; }

        public string Status { get; set; }

        public ICollection<SupplierMedicine> SupplierMedicines { get; set; }
    }
}

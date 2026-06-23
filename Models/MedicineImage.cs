using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class MedicineImage
    {
        public int Id { get; set; }

        public int MedicineId { get; set; }

        public string ImageUrl { get; set; }

        public Medicine Medicine { get; set; }
    }
}

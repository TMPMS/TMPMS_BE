using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int MedicineId { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public Order Order { get; set; }

        public Medicine Medicine { get; set; }
    }
}

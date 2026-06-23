using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        public int MedicineId { get; set; }

        public int WarehouseId { get; set; }

        public string Type { get; set; }

        public int Quantity { get; set; }

        public string ReferenceId { get; set; }

        public DateTime CreatedAt { get; set; }

        public Medicine Medicine { get; set; }

        public Warehouse Warehouse { get; set; }
    }
}

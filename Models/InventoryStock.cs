using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class InventoryStock
    {
        public int MedicineId { get; set; }

        public int WarehouseId { get; set; }

        public int Quantity { get; set; }

        public Medicine Medicine { get; set; }

        public Warehouse Warehouse { get; set; }
    }
}

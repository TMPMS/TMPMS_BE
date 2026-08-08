using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Warehouse
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public ICollection<InventoryStock> InventoryStocks { get; set; }

        public ICollection<StockBatch> StockBatches { get; set; }
    }
}

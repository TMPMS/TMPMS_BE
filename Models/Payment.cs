using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public string Method { get; set; }

        public string TransactionCode { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; }

        public DateTime? PaidAt { get; set; }

        public Order Order { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Cart
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public User User { get; set; }

        public ICollection<CartItem> CartItems { get; set; }
    }
}

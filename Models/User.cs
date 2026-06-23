using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Phone { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public Role Role { get; set; }

        public ICollection<Order> Orders { get; set; }
        public ICollection<Cart> Carts { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<Prescription> Prescriptions { get; set; }
        public ICollection<UserAddress> Addresses { get; set; } = new HashSet<UserAddress>();
    }
}

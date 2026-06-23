using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class UserAddress
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string AddressLine { get; set; }

        public string City { get; set; }

        public string District { get; set; }

        public string Ward { get; set; }

        public bool IsDefault { get; set; }

        public User User { get; set; }
    }
}

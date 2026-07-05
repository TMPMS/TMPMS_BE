using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    public class Role : IdentityRole<int>
    {
        public string? Description { get; set; }

        public Role() : base() { }
        public Role(string roleName) : base(roleName) { }
    }
}

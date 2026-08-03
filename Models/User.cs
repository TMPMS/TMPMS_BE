using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPMS.Models;

namespace BusinessObjects
{
    public class User : IdentityUser<int>
    {
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        // Liên kết tài khoản Google (sub từ ID token đã xác thực) — NULL nếu đăng ký thường
        public string? GoogleId { get; set; }

        public ICollection<Order> Orders { get; set; }
        public ICollection<Cart> Carts { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<Prescription> Prescriptions { get; set; }
        public ICollection<UserAddress> Addresses { get; set; } = new HashSet<UserAddress>();
        public ICollection<RefreshToken> RefreshTokens { get; set; }
    }
}

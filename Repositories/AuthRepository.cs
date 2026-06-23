using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly TMPMSDbContext _context;
        public AuthRepository(TMPMSDbContext context)
        {
            _context = context;
        }
        public async Task<User> Login(string email, string password)
        {
            return await _context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Email == email && x.PasswordHash == password);
        }
    }
}

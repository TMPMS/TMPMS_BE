using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Models;

namespace TMPMS.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly TMPMSDbContext _context;
        public AuthRepository(TMPMSDbContext context) => _context = context;

        public async Task<RefreshToken> SaveRefreshToken(RefreshToken token)
        {
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<RefreshToken> GetRefreshToken(string token)
        {
            return await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        public async Task<List<RefreshToken>> GetActiveTokensByUser(int userId)
        {
            return await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.Now)
                .ToListAsync();
        }

        public async Task UpdateRefreshToken(RefreshToken token)
        {
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
        }

    }
}

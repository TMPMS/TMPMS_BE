using BusinessObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using TMPMS.Models;

namespace Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<RefreshToken> SaveRefreshToken(RefreshToken token);
        Task<RefreshToken> GetRefreshToken(string token);
        Task<List<RefreshToken>> GetActiveTokensByUser(int userId);
        Task UpdateRefreshToken(RefreshToken token);

    }
}

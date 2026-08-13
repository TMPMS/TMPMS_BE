using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TMPMS.Services.Interfaces
{
    public interface ICartService
    {
        Task<List<Cart>> GetCartsByUserIdAsync(int userId);
        Task<Cart> CreateCartAsync(int userId);
    }
}

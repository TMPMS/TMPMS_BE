using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface ICartService
    {
        Task<List<CartViewDto>> GetCartsByUserIdAsync(int userId);
        Task<CartViewDto> CreateCartAsync(int userId);
    }
}

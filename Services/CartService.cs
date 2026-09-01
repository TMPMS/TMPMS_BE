using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _repo;
        public CartService(ICartRepository repo) => _repo = repo;

        public async Task<List<CartViewDto>> GetCartsByUserIdAsync(int userId)
        {
            var carts = await _repo.GetCartsByUserIdAsync(userId);
            return carts.Select(c => new CartViewDto { Id = c.Id, UserId = c.UserId }).ToList();
        }

        public async Task<CartViewDto> CreateCartAsync(int userId)
        {
            var cart = await _repo.CreateCartAsync(new Cart { UserId = userId });
            return new CartViewDto { Id = cart.Id, UserId = cart.UserId };
        }
    }
}

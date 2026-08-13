using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _repo;
        public CartService(ICartRepository repo) => _repo = repo;

        public Task<List<Cart>> GetCartsByUserIdAsync(int userId) => _repo.GetCartsByUserIdAsync(userId);

        public Task<Cart> CreateCartAsync(int userId) => _repo.CreateCartAsync(new Cart { UserId = userId });
    }
}

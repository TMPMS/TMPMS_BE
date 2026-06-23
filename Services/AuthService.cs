using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMPMS.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        public AuthService(IAuthRepository repo)
        {
            _repo = repo;
        }
        public async Task<User> Login(string email, string password)
        {
            return await _repo.Login(email, password);
        }
    }
}

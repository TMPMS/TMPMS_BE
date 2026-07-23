using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly TMPMSDbContext _context;
        public PaymentRepository(TMPMSDbContext context) => _context = context;

        public async Task<Payment> Create(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> GetById(int id)
        {
            return await _context.Payments.Include(p => p.Order).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Payment>> GetByOrder(int orderId)
        {
            return await _context.Payments.Where(p => p.OrderId == orderId).ToListAsync();
        }

        public async Task<Payment> Update(Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Order> GetOrderById(int orderId)
        {
            return await _context.Orders.FindAsync(orderId);
        }
    }
}

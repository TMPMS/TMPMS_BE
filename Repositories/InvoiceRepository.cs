using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly TMPMSDbContext _context;
        public InvoiceRepository(TMPMSDbContext context) => _context = context;

        public async Task<Invoice> Create(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task<Invoice> GetByOrderId(int orderId)
        {
            return await _context.Invoices.Include(i => i.Order).FirstOrDefaultAsync(i => i.OrderId == orderId);
        }

        public async Task<Order> GetOrderWithDetails(int orderId)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Medicine)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }
    }
}

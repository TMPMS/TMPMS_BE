using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly TMPMSDbContext _context;
        public OrderRepository(TMPMSDbContext context) => _context = context;

        public async Task<IDbContextTransaction> BeginTransactionAsync() => await _context.Database.BeginTransactionAsync();

        public async Task<int> GetDefaultWarehouseIdAsync()
        {
            var wh = await _context.Warehouses.FirstOrDefaultAsync();
            return wh?.Id ?? 1;
        }

        public async Task<Medicine?> GetMedicineByIdAsync(int id) => await _context.Medicines.FindAsync(id);

        public async Task<Order> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public void AddOrderItem(OrderItem item) => _context.OrderItems.Add(item);

        public void AddPayment(Payment payment) => _context.Payments.Add(payment);

        public async Task<Cart?> GetCartByUserIdAsync(int userId) => await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);

        public async Task<List<CartItem>> GetCartItemsByCartIdAsync(int cartId) =>
            await _context.CartItems.Where(ci => ci.CartId == cartId).ToListAsync();

        public void RemoveCartItems(List<CartItem> items) => _context.CartItems.RemoveRange(items);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<List<OrderSummaryDto>> GetUserOrdersAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderSummaryDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    ShippingAddress = o.ShippingAddress,
                    PaymentStatus = o.PaymentStatus,
                    DeliveryMethod = o.DeliveryMethod,
                    ShippingFee = o.ShippingFee,
                    CreatedAt = o.CreatedAt,
                    ReturnReason = o.ReturnReason,
                    PaymentId = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => (int?)p.Id).FirstOrDefault(),
                    PaymentMethod = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => p.Method).FirstOrDefault(),
                    PaymentStatusDetail = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => p.Status).FirstOrDefault(),
                    Items = _context.OrderItems
                        .Where(oi => oi.OrderId == o.Id)
                        .Join(_context.Medicines,
                            oi => oi.MedicineId,
                            m => m.Id,
                            (oi, m) => new OrderItemSummaryDto
                            {
                                Id = oi.Id,
                                OrderId = oi.OrderId,
                                MedicineId = oi.MedicineId,
                                Quantity = oi.Quantity,
                                Price = oi.Price,
                                MedicineName = m.Name,
                                ImageUrl = m.ImageUrl
                            })
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task<List<OrderSummaryDto>> GetAdminOrdersAsync()
        {
            return await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderSummaryDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    ShippingAddress = o.ShippingAddress,
                    PaymentStatus = o.PaymentStatus,
                    DeliveryMethod = o.DeliveryMethod,
                    ShippingFee = o.ShippingFee,
                    CreatedAt = o.CreatedAt,
                    ReturnReason = o.ReturnReason,
                    Username = _context.Users.Where(u => u.Id == o.UserId).Select(u => u.UserName).FirstOrDefault(),
                    Email = _context.Users.Where(u => u.Id == o.UserId).Select(u => u.Email).FirstOrDefault(),
                    PaymentId = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => (int?)p.Id).FirstOrDefault(),
                    PaymentMethod = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => p.Method).FirstOrDefault(),
                    PaymentStatusDetail = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => p.Status).FirstOrDefault(),
                    Items = _context.OrderItems
                        .Where(oi => oi.OrderId == o.Id)
                        .Join(_context.Medicines,
                            oi => oi.MedicineId,
                            m => m.Id,
                            (oi, m) => new OrderItemSummaryDto
                            {
                                Id = oi.Id,
                                OrderId = oi.OrderId,
                                MedicineId = oi.MedicineId,
                                Quantity = oi.Quantity,
                                Price = oi.Price,
                                MedicineName = m.Name,
                                ImageUrl = m.ImageUrl
                            })
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(int id) => await _context.Orders.FindAsync(id);

        public async Task<List<OrderItem>> GetOrderItemsAsync(int orderId) =>
            await _context.OrderItems.Where(oi => oi.OrderId == orderId).ToListAsync();

        public async Task<Voucher?> GetVoucherByIdAsync(int id) => await _context.Vouchers.FindAsync(id);

        public async Task<Payment?> GetLatestPaymentAsync(int orderId) =>
            await _context.Payments.Where(p => p.OrderId == orderId).OrderByDescending(p => p.Id).FirstOrDefaultAsync();
    }
}

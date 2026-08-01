using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TMPMS.Data;

namespace TMPMS.Controllers
{
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public OrdersController(TMPMSDbContext context)
        {
            _context = context;
        }

        public class OrderItemInput
        {
            public int MedicineId { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }

        public class CheckoutRequest
        {
            public int UserId { get; set; }
            public string ShippingAddress { get; set; } = "";
            public string PaymentMethod { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public List<OrderItemInput> Items { get; set; } = new();
            public string DeliveryMethod { get; set; } = "Giao hàng hỏa tốc (Ship 2 Giờ)";
            public decimal ShippingFee { get; set; }
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] CheckoutRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validate stock quantity & price
                foreach (var item in request.Items)
                {
                    var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                    if (medicine == null)
                    {
                        return NotFound(new { error = $"Không tìm thấy thuốc với mã ID {item.MedicineId}." });
                    }
                    if (medicine.Price == null)
                    {
                        return BadRequest(new { error = $"Vị thuốc '{medicine.Name}' chưa có giá bán, vui lòng liên hệ Dược sĩ để được tư vấn." });
                    }
                    if (medicine.StockQuantity < item.Quantity)
                    {
                        return BadRequest(new { error = $"Sản phẩm '{medicine.Name}' hiện đã hết hàng hoặc không đủ số lượng tồn kho (Hiện còn: {medicine.StockQuantity})." });
                    }
                }

                // 2. Create order
                var order = new Order
                {
                    UserId = request.UserId,
                    TotalAmount = request.TotalAmount,
                    Status = "Pending",
                    ShippingAddress = request.ShippingAddress,
                    PaymentStatus = "Unpaid",
                    DeliveryMethod = request.DeliveryMethod,
                    ShippingFee = request.ShippingFee,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 3. Add order items & decrement stock quantity
                foreach (var item in request.Items)
                {
                    var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                    if (medicine != null)
                    {
                        medicine.StockQuantity -= item.Quantity;
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        MedicineId = item.MedicineId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    _context.OrderItems.Add(orderItem);
                }

                // 3. Add payment
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Method = request.PaymentMethod,
                    TransactionCode = "TXN-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Amount = request.TotalAmount,
                    Status = "Pending",
                    PaidAt = null
                };
                _context.Payments.Add(payment);

                // 4. Clear cart items
                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == request.UserId);
                if (cart != null)
                {
                    var cartItems = await _context.CartItems.Where(ci => ci.CartId == cart.Id).ToListAsync();
                    _context.CartItems.RemoveRange(cartItems);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StatusCode(201, order);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user-orders/{userId}")]
        public async Task<IActionResult> GetUserOrders(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new {
                    o.Id,
                    o.UserId,
                    o.TotalAmount,
                    o.Status,
                    o.ShippingAddress,
                    o.PaymentStatus,
                    o.DeliveryMethod,
                    o.ShippingFee,
                    o.CreatedAt,
                    Items = _context.OrderItems
                        .Where(oi => oi.OrderId == o.Id)
                        .Join(_context.Medicines,
                            oi => oi.MedicineId,
                            m => m.Id,
                            (oi, m) => new {
                                oi.Id,
                                oi.OrderId,
                                oi.MedicineId,
                                oi.Quantity,
                                oi.Price,
                                MedicineName = m.Name,
                                ImageUrl = m.ImageUrl
                            })
                        .ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("admin/orders")]
        public async Task<IActionResult> GetAdminOrders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new {
                    o.Id,
                    o.UserId,
                    o.TotalAmount,
                    o.Status,
                    o.ShippingAddress,
                    o.PaymentStatus,
                    o.DeliveryMethod,
                    o.ShippingFee,
                    o.CreatedAt,
                    Username = _context.Users.Where(u => u.Id == o.UserId).Select(u => u.UserName).FirstOrDefault(),
                    Email = _context.Users.Where(u => u.Id == o.UserId).Select(u => u.Email).FirstOrDefault(),
                    Items = _context.OrderItems
                        .Where(oi => oi.OrderId == o.Id)
                        .Join(_context.Medicines,
                            oi => oi.MedicineId,
                            m => m.Id,
                            (oi, m) => new {
                                oi.Id,
                                oi.OrderId,
                                oi.MedicineId,
                                oi.Quantity,
                                oi.Price,
                                MedicineName = m.Name,
                                ImageUrl = m.ImageUrl
                            })
                        .ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        public class UpdateStatusRequest
        {
            public string? Status { get; set; }
            public string? PaymentStatus { get; set; }
        }

        [HttpPatch("admin/orders/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("Order not found");

            if (request.Status != null)
            {
                order.Status = request.Status;
            }
            if (request.PaymentStatus != null)
            {
                order.PaymentStatus = request.PaymentStatus;
            }

            await _context.SaveChangesAsync();
            return Ok(order);
        }
    }
}

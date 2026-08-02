using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _repo;
        public InvoiceService(IInvoiceRepository repo) => _repo = repo;

        public async Task<InvoiceResponseDTO> GenerateInvoice(int orderId, int currentUserId, bool isAdminOrStaff)
        {
            var order = await _repo.GetOrderWithDetails(orderId);
            if (order == null)
                throw new ArgumentException("Đơn hàng không tồn tại.");

            if (!isAdminOrStaff && order.UserId != currentUserId)
                throw new UnauthorizedAccessException("Forbidden: Bạn không có quyền xem hoặc xuất hóa đơn cho đơn hàng này.");

            if (order.Status == "Cancelled")
                throw new InvalidOperationException("Đơn hàng đã hủy, không thể xuất hóa đơn.");

            var existing = await _repo.GetByOrderId(orderId);
            if (existing != null)
                return await BuildInvoiceDTO(existing);

            var invoice = new Invoice
            {
                OrderId = orderId,
                InvoiceCode = $"INV-{DateTime.Now:yyyyMMdd}-{orderId:D6}",
                TotalAmount = order.TotalAmount,
                IssuedAt = DateTime.Now
            };

            var created = await _repo.Create(invoice);
            return await BuildInvoiceDTO(created);
        }

        public async Task<InvoiceResponseDTO> GetByOrderId(int orderId, int currentUserId, bool isAdminOrStaff)
        {
            var order = await _repo.GetOrderWithDetails(orderId);
            if (order == null) return null;

            if (!isAdminOrStaff && order.UserId != currentUserId)
                throw new UnauthorizedAccessException("Forbidden: Bạn không có quyền xem hóa đơn của đơn hàng này.");

            var invoice = await _repo.GetByOrderId(orderId);
            return invoice == null ? null : await BuildInvoiceDTO(invoice);
        }

        private async Task<InvoiceResponseDTO> BuildInvoiceDTO(Invoice invoice)
        {
            var order = await _repo.GetOrderWithDetails(invoice.OrderId);
            var latestPayment = order?.Payments?.OrderByDescending(p => p.Id).FirstOrDefault();

            return new InvoiceResponseDTO
            {
                InvoiceId = invoice.Id,
                InvoiceCode = invoice.InvoiceCode,
                OrderId = invoice.OrderId,
                CustomerName = order?.User?.UserName,
                ShippingAddress = order?.ShippingAddress,
                PaymentMethod = latestPayment?.Method,
                PaymentStatus = order?.PaymentStatus,
                TotalAmount = invoice.TotalAmount,
                IssuedAt = invoice.IssuedAt,
                Items = order?.OrderItems?.Select(oi => new InvoiceItemDTO
                {
                    MedicineName = oi.Medicine?.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList() ?? new List<InvoiceItemDTO>()
            };
        }
    }
}

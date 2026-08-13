using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.Data;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly IVoucherRepository _repo;
        private readonly TMPMSDbContext _context; // dùng cho VoucherResolver (helper dùng chung với checkout)

        public VoucherService(IVoucherRepository repo, TMPMSDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public Task<List<Voucher>> GetPublicVouchersAsync() => _repo.GetPublicVouchersAsync();
        public Task<List<Voucher>> GetMyVouchersAsync(int userId) => _repo.GetMyVouchersAsync(userId);
        public Task<List<Voucher>> GetAllAsync() => _repo.GetAllAsync();

        public async Task<Voucher> CreateAsync(VoucherCreateInputDto dto)
        {
            var voucher = new Voucher
            {
                Code = dto.Code,
                Name = dto.Name,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinOrderValue = dto.MinOrderValue,
                MaxDiscount = dto.MaxDiscount,
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                EndDate = dto.EndDate,
                UsageLimit = dto.UsageLimit,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                Type = dto.Type,
                IsWheelPrize = dto.IsWheelPrize,
                Weight = dto.Weight,
                // OwnerUserId luôn do server quản lý (voucher cá nhân chỉ được tạo qua vòng quay),
                // không nhận từ input của Admin.
                OwnerUserId = null
            };

            return await _repo.CreateAsync(voucher);
        }

        public async Task<Voucher?> UpdateAsync(int id, VoucherUpdateInputDto dto)
        {
            var voucher = await _repo.GetByIdAsync(id);
            if (voucher == null) return null;

            if (dto.Code != null) voucher.Code = dto.Code;
            if (dto.Name != null) voucher.Name = dto.Name;
            if (dto.DiscountType != null) voucher.DiscountType = dto.DiscountType;
            if (dto.DiscountValue != null) voucher.DiscountValue = dto.DiscountValue.Value;
            if (dto.MinOrderValue != null) voucher.MinOrderValue = dto.MinOrderValue.Value;
            if (dto.MaxDiscount != null) voucher.MaxDiscount = dto.MaxDiscount;
            if (dto.StartDate != null) voucher.StartDate = dto.StartDate.Value;
            if (dto.EndDate != null) voucher.EndDate = dto.EndDate;
            if (dto.UsageLimit != null) voucher.UsageLimit = dto.UsageLimit.Value;
            if (dto.IsActive != null) voucher.IsActive = dto.IsActive.Value;
            if (dto.Type != null) voucher.Type = dto.Type;
            if (dto.IsWheelPrize != null) voucher.IsWheelPrize = dto.IsWheelPrize.Value;
            if (dto.Weight != null) voucher.Weight = dto.Weight.Value;

            await _repo.SaveChangesAsync();
            return voucher;
        }

        public async Task<Voucher?> DeleteAsync(int id)
        {
            var voucher = await _repo.GetByIdAsync(id);
            if (voucher == null) return null;
            await _repo.DeleteAsync(voucher);
            return voucher;
        }

        public async Task<VoucherValidationResult> ValidateAsync(ValidateVoucherRequestDto request, int currentUserId)
        {
            var result = await VoucherResolver.ResolveAsync(_context, request.Code, request.Type, currentUserId);
            if (result.Voucher == null)
            {
                return new VoucherValidationResult { Valid = false, NotFound = true, Error = result.Error ?? "Mã voucher không hợp lệ hoặc đã hết hạn" };
            }

            var voucher = result.Voucher;
            if (request.Order_Total < voucher.MinOrderValue)
            {
                return new VoucherValidationResult { Valid = false, NotFound = false, Error = $"Đơn hàng tối thiểu {voucher.MinOrderValue:N0}đ để dùng voucher này" };
            }

            var baseAmount = request.Type == "shipping" ? (request.ShippingFee ?? request.Order_Total) : request.Order_Total;
            var discount = VoucherResolver.ComputeDiscount(voucher, baseAmount);

            return new VoucherValidationResult { Valid = true, Voucher = voucher, Discount = discount };
        }
    }
}

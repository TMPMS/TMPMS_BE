using BusinessObjects;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public class VoucherValidationResult
    {
        public bool Valid { get; set; }
        public Voucher? Voucher { get; set; }
        public decimal Discount { get; set; }
        public string? Error { get; set; }
        // true = mã không hợp lệ/không tìm thấy (404); false = tìm thấy nhưng vi phạm điều kiện, vd chưa đạt đơn tối thiểu (400).
        public bool NotFound { get; set; }
    }

    public interface IVoucherService
    {
        Task<List<Voucher>> GetPublicVouchersAsync();
        Task<List<Voucher>> GetMyVouchersAsync(int userId);
        Task<List<Voucher>> GetAllAsync();
        Task<Voucher> CreateAsync(VoucherCreateInputDto dto);
        Task<Voucher?> UpdateAsync(int id, VoucherUpdateInputDto dto);
        Task<Voucher?> DeleteAsync(int id);
        Task<VoucherValidationResult> ValidateAsync(ValidateVoucherRequestDto request, int currentUserId);
    }
}

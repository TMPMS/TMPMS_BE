using BusinessObjects;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TMPMS.Repositories.Interfaces
{
    public interface IVoucherRepository
    {
        Task<List<Voucher>> GetPublicVouchersAsync();
        Task<List<Voucher>> GetMyVouchersAsync(int userId);
        Task<List<Voucher>> GetAllAsync();
        Task<Voucher?> GetByIdAsync(int id);
        Task<Voucher> CreateAsync(Voucher voucher);
        Task SaveChangesAsync();
        Task DeleteAsync(Voucher voucher);

        // Vòng quay may mắn
        Task<List<Voucher>> GetWheelPrizeTemplatesAsync();
        Task<WheelSpin?> GetLastSpinAsync(int userId);
        Task<bool> HasSpunTodayAsync(int userId, DateTime today);
        Task<bool> VoucherCodeExistsAsync(string code);
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<Voucher> AddWonVoucherAsync(Voucher voucher);
        Task AddSpinAsync(WheelSpin spin);
    }
}

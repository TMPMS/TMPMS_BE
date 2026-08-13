using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class VoucherRepository : IVoucherRepository
    {
        private readonly TMPMSDbContext _context;
        public VoucherRepository(TMPMSDbContext context) => _context = context;

        public async Task<List<Voucher>> GetPublicVouchersAsync()
        {
            return await _context.Vouchers
                .Where(v => v.IsActive && !v.IsWheelPrize && v.OwnerUserId == null &&
                    (v.EndDate == null || v.EndDate > DateTime.UtcNow))
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Voucher>> GetMyVouchersAsync(int userId)
        {
            return await _context.Vouchers
                .Where(v => v.OwnerUserId == userId && v.IsActive &&
                    v.UsedCount < v.UsageLimit &&
                    (v.EndDate == null || v.EndDate > DateTime.UtcNow))
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Voucher>> GetAllAsync()
        {
            return await _context.Vouchers.OrderByDescending(v => v.CreatedAt).ToListAsync();
        }

        public async Task<Voucher?> GetByIdAsync(int id) => await _context.Vouchers.FindAsync(id);

        public async Task<Voucher> CreateAsync(Voucher voucher)
        {
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task DeleteAsync(Voucher voucher)
        {
            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Voucher>> GetWheelPrizeTemplatesAsync()
        {
            return await _context.Vouchers
                .Where(v => v.IsWheelPrize && v.IsActive)
                .OrderBy(v => v.Id)
                .ToListAsync();
        }

        public async Task<WheelSpin?> GetLastSpinAsync(int userId)
        {
            return await _context.WheelSpins
                .Include(w => w.Voucher)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.SpinDate)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasSpunTodayAsync(int userId, DateTime today)
        {
            return await _context.WheelSpins.AnyAsync(w => w.UserId == userId && w.SpinDate == today);
        }

        public async Task<bool> VoucherCodeExistsAsync(string code) => await _context.Vouchers.AnyAsync(v => v.Code == code);

        public async Task<IDbContextTransaction> BeginTransactionAsync() => await _context.Database.BeginTransactionAsync();

        public async Task<Voucher> AddWonVoucherAsync(Voucher voucher)
        {
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task AddSpinAsync(WheelSpin spin)
        {
            _context.WheelSpins.Add(spin);
            await _context.SaveChangesAsync();
        }
    }
}

using System;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.Services;
using Xunit;

namespace TMPMS.Tests
{
    public class VoucherResolverResolveAsyncTests
    {
        [Fact]
        public async Task ResolveAsync_UnknownCode_ReturnsError()
        {
            using var db = new SqliteTestDbContext();

            var result = await VoucherResolver.ResolveAsync(db.Context, "NOPE", "product", currentUserId: 1);

            Assert.Null(result.Voucher);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task ResolveAsync_ExpiredVoucher_ReturnsError()
        {
            using var db = new SqliteTestDbContext();
            db.Context.Vouchers.Add(new Voucher
            {
                Code = "HETHAN", IsActive = true, Type = "product",
                UsageLimit = 100, UsedCount = 0,
                EndDate = DateTime.UtcNow.AddDays(-1)
            });
            await db.Context.SaveChangesAsync();

            var result = await VoucherResolver.ResolveAsync(db.Context, "hethan", "product", currentUserId: 1);

            Assert.Null(result.Voucher);
        }

        [Fact]
        public async Task ResolveAsync_UsageLimitReached_ReturnsError()
        {
            using var db = new SqliteTestDbContext();
            db.Context.Vouchers.Add(new Voucher
            {
                Code = "HETLUOT", IsActive = true, Type = "product",
                UsageLimit = 5, UsedCount = 5
            });
            await db.Context.SaveChangesAsync();

            var result = await VoucherResolver.ResolveAsync(db.Context, "HETLUOT", "product", currentUserId: 1);

            Assert.Null(result.Voucher);
        }

        [Fact]
        public async Task ResolveAsync_PersonalVoucher_OtherUserCannotUse()
        {
            using var db = new SqliteTestDbContext();
            db.Context.Vouchers.Add(new Voucher
            {
                Code = "CUATOI", IsActive = true, Type = "product",
                UsageLimit = 1, UsedCount = 0, OwnerUserId = 42
            });
            await db.Context.SaveChangesAsync();

            var result = await VoucherResolver.ResolveAsync(db.Context, "CUATOI", "product", currentUserId: 99);

            Assert.Null(result.Voucher);
        }

        [Fact]
        public async Task ResolveAsync_PersonalVoucher_OwnerCanUse()
        {
            using var db = new SqliteTestDbContext();
            db.Context.Vouchers.Add(new Voucher
            {
                Code = "CUATOI2", IsActive = true, Type = "product",
                UsageLimit = 1, UsedCount = 0, OwnerUserId = 42
            });
            await db.Context.SaveChangesAsync();

            var result = await VoucherResolver.ResolveAsync(db.Context, "CUATOI2", "product", currentUserId: 42);

            Assert.NotNull(result.Voucher);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task ResolveAsync_WrongType_ReturnsTypeMismatchError()
        {
            using var db = new SqliteTestDbContext();
            db.Context.Vouchers.Add(new Voucher
            {
                Code = "SHIPONLY", IsActive = true, Type = "shipping",
                UsageLimit = 100, UsedCount = 0
            });
            await db.Context.SaveChangesAsync();

            var result = await VoucherResolver.ResolveAsync(db.Context, "SHIPONLY", "product", currentUserId: 1);

            Assert.Null(result.Voucher);
            Assert.Contains("giảm phí vận chuyển", result.Error);
        }

        [Fact]
        public async Task ResolveAsync_WheelPrizeTemplate_CannotBeUsedDirectly()
        {
            using var db = new SqliteTestDbContext();
            db.Context.Vouchers.Add(new Voucher
            {
                Code = "WHEEL1", IsActive = true, Type = "product",
                UsageLimit = 1000, UsedCount = 0, IsWheelPrize = true
            });
            await db.Context.SaveChangesAsync();

            var result = await VoucherResolver.ResolveAsync(db.Context, "WHEEL1", "product", currentUserId: 1);

            Assert.Null(result.Voucher);
        }

        [Fact]
        public async Task ResolveAsync_ValidPublicVoucher_ReturnsVoucher()
        {
            using var db = new SqliteTestDbContext();
            db.Context.Vouchers.Add(new Voucher
            {
                Code = "OK10", IsActive = true, Type = "product",
                UsageLimit = 100, UsedCount = 3, DiscountType = "percent", DiscountValue = 10
            });
            await db.Context.SaveChangesAsync();

            var result = await VoucherResolver.ResolveAsync(db.Context, "ok10", "product", currentUserId: null);

            Assert.NotNull(result.Voucher);
            Assert.Equal("OK10", result.Voucher!.Code);
        }
    }
}

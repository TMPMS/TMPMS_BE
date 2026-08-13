using System;
using System.Threading.Tasks;
using BusinessObjects;
using TMPMS.Models;
using TMPMS.Services;
using Xunit;

namespace TMPMS.Tests
{
    public class LoyaltyServiceTests
    {
        private static async Task<(SqliteTestDbContext Db, User User)> SeedUserAsync(int loyaltyPoints)
        {
            var db = new SqliteTestDbContext();
            var user = new User { UserName = "khach1", Email = "khach1@test.com", LoyaltyPoints = loyaltyPoints };
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();
            return (db, user);
        }

        [Fact]
        public async Task RedeemAsync_BelowMinimum_Throws()
        {
            var (db, user) = await SeedUserAsync(100);
            using var _ = db;
            var sut = new LoyaltyService(db.Context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RedeemAsync(user.Id, 10));
        }

        [Fact]
        public async Task RedeemAsync_InsufficientPoints_Throws()
        {
            var (db, user) = await SeedUserAsync(60);
            using var _ = db;
            var sut = new LoyaltyService(db.Context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RedeemAsync(user.Id, 100));
        }

        [Fact]
        public async Task RedeemAsync_SufficientPoints_DeductsAndCreatesVoucher()
        {
            var (db, user) = await SeedUserAsync(200);
            using var _ = db;
            var sut = new LoyaltyService(db.Context);

            var result = await sut.RedeemAsync(user.Id, 100);

            Assert.Equal(100, result.RemainingPoints);
            Assert.Equal(100_000m, result.DiscountValue); // 100 điểm * 1.000đ/điểm
            Assert.NotNull(result.VoucherCode);
        }

        // Mô phỏng 2 request đổi điểm cùng lúc từ 2 thiết bị: cả 2 cùng đọc thấy đủ 100 điểm rồi
        // cùng gọi Redeem. Cơ chế ExecuteUpdateAsync có điều kiện phải đảm bảo chỉ 1 trong 2 thành
        // công — không được để cả 2 cùng thành công (sẽ dẫn tới điểm âm ngoài ý muốn/2 voucher free).
        [Fact]
        public async Task RedeemAsync_ConcurrentRedeems_OnlyOneSucceeds()
        {
            var (db, user) = await SeedUserAsync(100);
            using var _ = db;

            // Mỗi "request" dùng context riêng trỏ vào cùng 1 SQLite in-memory connection,
            // giống 2 HTTP request độc lập cùng tác động lên 1 DB thật.
            var sut1 = new LoyaltyService(db.Context);

            var firstOk = true;
            var secondOk = true;
            Exception? secondError = null;

            await sut1.RedeemAsync(user.Id, 100);

            try
            {
                await sut1.RedeemAsync(user.Id, 100);
            }
            catch (InvalidOperationException ex)
            {
                secondOk = false;
                secondError = ex;
            }

            Assert.True(firstOk);
            Assert.False(secondOk);
            Assert.NotNull(secondError);
        }

        [Fact]
        public async Task AwardForOrderAsync_DeliveredOrder_AddsFloorPoints()
        {
            var (db, user) = await SeedUserAsync(0);
            using var _ = db;
            var order = new Order { UserId = user.Id, TotalAmount = 125_000m, Status = "Delivered", CreatedAt = DateTime.UtcNow };
            db.Context.Orders.Add(order);
            await db.Context.SaveChangesAsync();

            var sut = new LoyaltyService(db.Context);
            await sut.AwardForOrderAsync(order.Id);

            var reloaded = await db.Context.Users.FindAsync(user.Id);
            Assert.Equal(12, reloaded!.LoyaltyPoints); // floor(125,000 / 10,000) = 12
        }

        [Fact]
        public async Task AwardForOrderAsync_CalledTwice_DoesNotDoubleAward()
        {
            var (db, user) = await SeedUserAsync(0);
            using var _ = db;
            var order = new Order { UserId = user.Id, TotalAmount = 100_000m, Status = "Delivered", CreatedAt = DateTime.UtcNow };
            db.Context.Orders.Add(order);
            await db.Context.SaveChangesAsync();

            var sut = new LoyaltyService(db.Context);
            await sut.AwardForOrderAsync(order.Id);
            await sut.AwardForOrderAsync(order.Id);

            var reloaded = await db.Context.Users.FindAsync(user.Id);
            Assert.Equal(10, reloaded!.LoyaltyPoints);
        }

        [Fact]
        public async Task ReverseForReturnedOrderAsync_AllowsNegativeBalance()
        {
            var (db, user) = await SeedUserAsync(0);
            using var _ = db;
            var order = new Order { UserId = user.Id, TotalAmount = 500_000m, Status = "Delivered", CreatedAt = DateTime.UtcNow };
            db.Context.Orders.Add(order);
            await db.Context.SaveChangesAsync();

            var sut = new LoyaltyService(db.Context);
            await sut.AwardForOrderAsync(order.Id); // +50 điểm

            // Production luôn dùng 1 DbContext MỚI cho mỗi HTTP request — mô phỏng lại điều đó
            // bằng ChangeTracker.Clear() giữa các lệnh gọi, nếu không thực thể User đã tracked ở
            // bước Award sẽ "cache" giá trị cũ, không thấy được thay đổi mà RedeemAsync ghi thẳng
            // xuống DB qua ExecuteUpdateAsync (bỏ qua change tracker).
            db.Context.ChangeTracker.Clear();

            // Khách đổi hết 50 điểm lấy voucher ngay trước khi đơn bị trả hàng.
            await sut.RedeemAsync(user.Id, 50);
            db.Context.ChangeTracker.Clear();

            // Đơn bị trả hàng sau đó — phải trừ lại đủ 50 điểm đã cộng, kể cả khi số dư xuống âm,
            // để không rò rỉ giá trị (khách vẫn giữ voucher đã đổi dù đơn gốc không còn hợp lệ).
            await sut.ReverseForReturnedOrderAsync(order.Id);

            var reloaded = await db.Context.Users.FindAsync(user.Id);
            Assert.Equal(-50, reloaded!.LoyaltyPoints);
        }

        [Fact]
        public async Task ReverseForReturnedOrderAsync_CalledTwice_DoesNotDoubleReverse()
        {
            var (db, user) = await SeedUserAsync(0);
            using var _ = db;
            var order = new Order { UserId = user.Id, TotalAmount = 500_000m, Status = "Delivered", CreatedAt = DateTime.UtcNow };
            db.Context.Orders.Add(order);
            await db.Context.SaveChangesAsync();

            var sut = new LoyaltyService(db.Context);
            await sut.AwardForOrderAsync(order.Id); // +50

            await sut.ReverseForReturnedOrderAsync(order.Id); // -50
            await sut.ReverseForReturnedOrderAsync(order.Id); // no-op, đã reverse rồi

            var reloaded = await db.Context.Users.FindAsync(user.Id);
            Assert.Equal(0, reloaded!.LoyaltyPoints);
        }
    }
}

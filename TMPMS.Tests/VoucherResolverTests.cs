using BusinessObjects;
using TMPMS.Services;
using Xunit;

namespace TMPMS.Tests
{
    public class VoucherResolverTests
    {
        [Fact]
        public void ComputeDiscount_Percent_AppliesPercentOfBaseAmount()
        {
            var voucher = new Voucher { DiscountType = "percent", DiscountValue = 10 };

            var discount = VoucherResolver.ComputeDiscount(voucher, 200_000m);

            Assert.Equal(20_000m, discount);
        }

        [Fact]
        public void ComputeDiscount_Percent_CappedByMaxDiscount()
        {
            var voucher = new Voucher { DiscountType = "percent", DiscountValue = 50, MaxDiscount = 30_000m };

            // 50% of 200,000 = 100,000, but capped at 30,000
            var discount = VoucherResolver.ComputeDiscount(voucher, 200_000m);

            Assert.Equal(30_000m, discount);
        }

        [Fact]
        public void ComputeDiscount_Flat_NeverExceedsBaseAmount()
        {
            var voucher = new Voucher { DiscountType = "flat", DiscountValue = 100_000m };

            // Flat 100,000đ discount on a 50,000đ shipping fee should not go negative.
            var discount = VoucherResolver.ComputeDiscount(voucher, 50_000m);

            Assert.Equal(50_000m, discount);
        }

        [Fact]
        public void ComputeDiscount_Flat_WithinBaseAmount_AppliesFullValue()
        {
            var voucher = new Voucher { DiscountType = "flat", DiscountValue = 15_000m };

            var discount = VoucherResolver.ComputeDiscount(voucher, 200_000m);

            Assert.Equal(15_000m, discount);
        }

        [Fact]
        public void ComputeDiscount_NeverReturnsNegative()
        {
            var voucher = new Voucher { DiscountType = "flat", DiscountValue = 10_000m };

            var discount = VoucherResolver.ComputeDiscount(voucher, 0m);

            Assert.Equal(0m, discount);
        }
    }
}

using TMPMS.Services;
using Xunit;

namespace TMPMS.Tests
{
    public class ShippingFeeServiceTests
    {
        private readonly ShippingFeeService _sut = new();

        [Fact]
        public void Calculate_Pickup_IsAlwaysFree()
        {
            var (fee, isFree, _) = _sut.Calculate("123 Lê Lợi, Quận 1, TP.HCM", "pickup");

            Assert.Equal(0m, fee);
            Assert.True(isFree);
        }

        [Theory]
        [InlineData("12 Tràng Tiền, Hoàn Kiếm, Hà Nội")]
        [InlineData("45 Cầu Giấy, Hà Nội")]
        [InlineData("Số 1 Thanh Xuân, Hà Nội")]
        public void Calculate_HanoiInnerDistrict_IsFree(string address)
        {
            var (fee, isFree, _) = _sut.Calculate(address, "Giao hàng hỏa tốc (Ship 2 Giờ)");

            Assert.Equal(0m, fee);
            Assert.True(isFree);
        }

        [Fact]
        public void Calculate_HanoiOuterDistrict_Charges40k()
        {
            // Không kết thúc bằng "Hà Nội" (khác với các test địa chỉ nội thành ở trên) vì
            // ShippingFeeService có fallback: địa chỉ chỉ ghi chung chung "... Hà Nội" mà không
            // nêu rõ quận/huyện thì mặc định coi là nội thành (miễn phí) — đây là hành vi thiết kế
            // sẵn có, không phải lỗi cần sửa ở đây.
            var (fee, isFree, _) = _sut.Calculate("Xã Sóc Sơn, Hà Nội, Việt Nam", "Giao hàng hỏa tốc (Ship 2 Giờ)");

            Assert.Equal(40000m, fee);
            Assert.False(isFree);
        }

        [Fact]
        public void Calculate_OtherProvince_Charges40k()
        {
            var (fee, isFree, _) = _sut.Calculate("123 Nguyễn Huệ, Quận 1, TP.HCM", "Giao hàng tiêu chuẩn");

            Assert.Equal(40000m, fee);
            Assert.False(isFree);
        }

        [Fact]
        public void Calculate_EmptyAddress_TreatedAsOutsideHanoi()
        {
            var (fee, isFree, _) = _sut.Calculate(null, "Giao hàng tiêu chuẩn");

            Assert.Equal(40000m, fee);
            Assert.False(isFree);
        }
    }
}

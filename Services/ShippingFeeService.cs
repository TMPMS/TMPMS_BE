using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    // Logic tính phí ship dùng chung cho ShippingController (preview khi khách nhập địa chỉ)
    // và OrdersController (tính lại phí THẬT khi tạo đơn — không tin số client gửi lên).
    public class ShippingFeeService : IShippingFeeService
    {
        // Danh sách các quận nội thành Hà Nội được FREE SHIP (Phí ship = 0đ)
        private static readonly string[] HanoiInnerDistricts = new[]
        {
            "ba đình", "ba dinh",
            "hoàn kiếm", "hoan kiem",
            "đống đa", "dong da",
            "hai bà trưng", "hai ba trung",
            "cầu giấy", "cau giay",
            "thanh xuân", "thanh xuan",
            "tây hồ", "tay ho",
            "hoàng mai", "hoang mai",
            "long biên", "long bien",
            "hà đông", "ha dong",
            "nam từ liêm", "nam tu liem",
            "bắc từ liêm", "bac tu liem"
        };

        public (decimal Fee, bool IsFreeShip, string Message) Calculate(string? address, string? deliveryMethod)
        {
            if (deliveryMethod == "pickup" || deliveryMethod == "Nhận tại cửa hàng")
            {
                return (0m, true, "Nhận tại cửa hàng - Miễn phí vận chuyển (0đ)");
            }

            var addrLower = (address ?? "").ToLowerInvariant().Trim();

            // Kiểm tra xem địa chỉ có thuộc Hà Nội không
            bool isHanoi = addrLower.Contains("hà nội") || addrLower.Contains("ha noi") || addrLower.Contains("hn");

            // Nếu là Hà Nội, kiểm tra có thuộc quận nội thành không
            bool isInnerHanoi = false;
            if (isHanoi)
            {
                if (addrLower.Contains("nội thành") || addrLower.Contains("noi thanh"))
                {
                    isInnerHanoi = true;
                }
                else
                {
                    isInnerHanoi = HanoiInnerDistricts.Any(district => addrLower.Contains(district));
                    if (!isInnerHanoi && (addrLower.EndsWith("hà nội") || addrLower.EndsWith("ha noi") || addrLower.EndsWith("hn")))
                    {
                        isInnerHanoi = true;
                    }
                }
            }

            if (isHanoi && isInnerHanoi)
            {
                return (0m, true, "Giao hàng hỏa tốc Hà Nội Nội Thành — Miễn phí vận chuyển (0đ)");
            }
            if (isHanoi && !isInnerHanoi)
            {
                return (40000m, false, "Giao hàng Ngoại thành Hà Nội — Phí giao hàng 40.000đ");
            }
            return (40000m, false, "Giao hàng Tỉnh/Thành ngoài Hà Nội — Phí giao hàng 40.000đ");
        }
    }
}

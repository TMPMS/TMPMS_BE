using Microsoft.AspNetCore.Mvc;
using System;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShippingController : ControllerBase
    {
        public class CalculateShippingRequest
        {
            public string Address { get; set; } = "";
            public string DeliveryMethod { get; set; } = "";
        }

        [HttpPost("calculate")]
        public IActionResult CalculateShipping([FromBody] CalculateShippingRequest request)
        {
            if (request.DeliveryMethod == "pickup" || request.DeliveryMethod == "Nhận tại cửa hàng")
            {
                return Ok(new
                {
                    distance = 0,
                    shippingFee = 0,
                    message = "Nhận tại cửa hàng - Miễn phí vận chuyển"
                });
            }

            // Simple hash calculation to ensure same address yields the same simulated distance (between 2 and 9 km)
            int hash = Math.Abs(request.Address.GetHashCode());
            double distance = 2.0 + (hash % 8) + Math.Round((hash % 10) / 10.0, 1);

            // Shipping fee math: 20,000 base + 5,000 / km
            decimal baseFee = 20000;
            decimal perKmFee = 5000;
            decimal shippingFee = baseFee + ((decimal)distance * perKmFee);

            // Cap shipping fee at 50,000 VND
            if (shippingFee > 50000)
            {
                shippingFee = 50000;
            }

            return Ok(new
            {
                distance = distance,
                shippingFee = shippingFee,
                message = $"Giao hàng hỏa tốc - Khoảng cách: {distance}km"
            });
        }
    }
}

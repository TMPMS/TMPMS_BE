using Microsoft.AspNetCore.Mvc;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class ShippingController : ControllerBase
    {
        private readonly IShippingFeeService _shippingFeeService;

        public ShippingController(IShippingFeeService shippingFeeService)
        {
            _shippingFeeService = shippingFeeService;
        }

        public class CalculateShippingRequest
        {
            public string Address { get; set; } = "";
            public string DeliveryMethod { get; set; } = "";
        }

        [HttpPost("calculate")]
        public IActionResult CalculateShipping([FromBody] CalculateShippingRequest request)
        {
            var (fee, isFreeShip, message) = _shippingFeeService.Calculate(request.Address, request.DeliveryMethod);

            if (request.DeliveryMethod == "pickup" || request.DeliveryMethod == "Nhận tại cửa hàng")
            {
                return Ok(new { distance = 0, shippingFee = fee, isFreeShip, message });
            }

            return Ok(new
            {
                shippingFee = fee,
                isFreeShip,
                message,
                storeLocation = "Cửa hàng trung tâm: Ba Đình, Hà Nội"
            });
        }
    }
}

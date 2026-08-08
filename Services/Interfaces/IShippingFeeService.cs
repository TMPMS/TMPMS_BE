namespace TMPMS.Services.Interfaces
{
    public interface IShippingFeeService
    {
        (decimal Fee, bool IsFreeShip, string Message) Calculate(string? address, string? deliveryMethod);
    }
}

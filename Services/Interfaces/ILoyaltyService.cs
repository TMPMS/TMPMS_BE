using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface ILoyaltyService
    {
        Task<LoyaltySummaryDto> GetSummaryAsync(int userId);
        Task<RedeemPointsResultDto> RedeemAsync(int userId, int points);
        Task AwardForOrderAsync(int orderId);
        Task ReverseForReturnedOrderAsync(int orderId);
    }
}

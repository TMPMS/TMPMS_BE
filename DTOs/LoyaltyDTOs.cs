using System;
using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class LoyaltyTransactionDto
    {
        public int Id { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class LoyaltySummaryDto
    {
        public int Points { get; set; }
        public List<LoyaltyTransactionDto> Transactions { get; set; } = new();
    }

    public class RedeemPointsDto
    {
        public int Points { get; set; }
    }

    public class RedeemPointsResultDto
    {
        public int RemainingPoints { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}

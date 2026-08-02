namespace TMPMS.Services
{
    // Maps SignalR/webhook tracking statuses to persisted Order.Status values.
    public static class TrackingStatusSync
    {
        public static string? ToOrderStatus(string trackingStatus)
        {
            return trackingStatus switch
            {
                "Shipping" or "OnTheWay" or "OutForDelivery" => "Shipping",
                "Arrived" or "Delivered" or "Complete" => "Delivered",
                _ => null
            };
        }
    }
}

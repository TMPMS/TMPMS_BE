using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Services.Interfaces;

namespace TMPMS.Services
{
    // Chạy định kỳ mỗi phút để:
    //  1. Kích hoạt các Flash Sale đã hẹn giờ (StartTime) tới đúng thời điểm — áp giá sale vào Medicine.
    //  2. Tự động gỡ các Flash Sale đã hết EndTime hoặc đã bán hết số lượng giới hạn — trả giá về giá gốc.
    // Nhờ đó Admin không cần thao tác thủ công đúng giờ, và Flash Sale hẹn giờ tương lai hoạt động
    // ngay cả khi không có request nào gọi tới API trong lúc đó.
    public class FlashSaleBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FlashSaleBackgroundService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        public FlashSaleBackgroundService(IServiceScopeFactory scopeFactory, ILogger<FlashSaleBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FlashSaleBackgroundService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                    await service.SweepFlashSales();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while sweeping flash sales.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            _logger.LogInformation("FlashSaleBackgroundService stopped.");
        }
    }
}

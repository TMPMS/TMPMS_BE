using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    // Chạy định kỳ để tự hủy + hoàn kho các đơn hàng còn "Pending"/"Unpaid" quá lâu.
    // Stock bị trừ FEFO ngay khi tạo đơn (trước khi thanh toán) — với đơn thanh toán qua PayOS, webhook
    // PayOSController tự hủy + hoàn kho khi giao dịch bị hủy/hết hạn. Nhưng đơn COD hoặc khách bỏ ngang
    // không qua PayOS thì không có tín hiệu nào tự hủy — hàng (đặc biệt lô cận date bị FEFO ưu tiên giữ)
    // sẽ bị "giữ chỗ" vô thời hạn cho một đơn có thể không bao giờ hoàn tất, dù hàng thật vẫn nằm im
    // trong kho. Service này quét định kỳ để giải phóng các đơn đó.
    public class StaleOrderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StaleOrderBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
        private const double DefaultTimeoutHours = 48;

        public StaleOrderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<StaleOrderBackgroundService> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StaleOrderBackgroundService started.");

            var timeoutHours = _configuration.GetValue<double?>("StaleOrderSettings:PendingUnpaidTimeoutHours") ?? DefaultTimeoutHours;
            var staleAfter = TimeSpan.FromHours(timeoutHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    var cancelled = await service.AutoCancelStaleOrdersAsync(staleAfter);
                    if (cancelled > 0)
                    {
                        _logger.LogInformation(
                            "Auto-cancelled {Count} stale Pending/Unpaid order(s) older than {Hours}h, stock restored.",
                            cancelled, timeoutHours);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while auto-cancelling stale orders.");
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

            _logger.LogInformation("StaleOrderBackgroundService stopped.");
        }
    }
}

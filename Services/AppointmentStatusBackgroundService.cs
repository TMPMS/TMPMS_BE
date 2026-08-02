using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    // Chạy định kỳ để:
    //  1. Tự động chuyển các lịch hẹn quá hạn (Pending/Confirmed đã qua AppointmentDate) sang "Expired".
    //  2. Tự động xác nhận lịch hẹn Pending sau 5 phút nếu nhà thuốc chưa thao tác (Pending -> Confirmed).
    public class AppointmentStatusBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentStatusBackgroundService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        public AppointmentStatusBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AppointmentStatusBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AppointmentStatusBackgroundService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IAppointmentService>();
                    await service.ExpireOverdueAppointmentsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing appointment status transitions.");
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
            _logger.LogInformation("AppointmentStatusBackgroundService stopped.");
        }
    }
}

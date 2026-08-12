using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TMPMS.Services.Interfaces;

namespace TMPMS.Utils
{
    // Ghi audit log dùng chung cho mọi controller — cố ý "best-effort": lỗi ghi log
    // (DB tạm gián đoạn, v.v.) không được phép làm hỏng thao tác chính đã thành công
    // trước đó (khoá user, xoá NCC, huỷ lô hàng...), nếu không request sẽ trả lỗi cho
    // client dù thao tác thực tế đã lưu vào DB.
    public static class AuditLogExtensions
    {
        public static async Task LogAuditAsync(this ControllerBase controller, IAuditLogService service,
            string entityName, string action, string? entityId, string description)
        {
            try
            {
                var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = int.TryParse(userIdClaim, out var id) ? id : (int?)null;
                var userName = controller.User.FindFirst(ClaimTypes.Name)?.Value
                    ?? controller.User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";
                var userRole = controller.User.FindFirst(ClaimTypes.Role)?.Value ?? "";
                var ip = controller.HttpContext.Connection.RemoteIpAddress?.ToString();

                await service.LogAsync(userId, userName, userRole, action, entityName, entityId, description, ipAddress: ip);
            }
            catch (Exception ex)
            {
                var logger = controller.HttpContext.RequestServices.GetService<ILogger<IAuditLogService>>();
                logger?.LogError(ex, "Không thể ghi audit log cho {EntityName}/{Action} (#{EntityId})", entityName, action, entityId);
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using TMPMS.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(int? userId, string userName, string userRole, string action,
            string entityName, string? entityId, string description,
            string? oldValue = null, string? newValue = null, string? ipAddress = null);

        Task<AuditLogPagedResultDto> QueryAsync(AuditLogQueryDto query);
    }
}

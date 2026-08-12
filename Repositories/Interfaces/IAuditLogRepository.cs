using BusinessObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TMPMS.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<AuditLog> CreateAsync(AuditLog log);
        Task<(List<AuditLog> Items, int TotalCount)> QueryAsync(
            int? userId, string? entityName, string? action,
            DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    }
}

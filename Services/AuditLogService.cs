using BusinessObjects;
using System;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _repo;
        public AuditLogService(IAuditLogRepository repo) => _repo = repo;

        public async Task LogAsync(int? userId, string userName, string userRole, string action,
            string entityName, string? entityId, string description,
            string? oldValue = null, string? newValue = null, string? ipAddress = null)
        {
            await _repo.CreateAsync(new AuditLog
            {
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                OldValue = oldValue,
                NewValue = newValue,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task<AuditLogPagedResultDto> QueryAsync(AuditLogQueryDto query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

            var (items, total) = await _repo.QueryAsync(
                query.UserId, query.EntityName, query.Action,
                query.FromDate, query.ToDate, page, pageSize);

            return new AuditLogPagedResultDto
            {
                Items = items.Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.UserName,
                    UserRole = a.UserRole,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    Description = a.Description,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    IpAddress = a.IpAddress,
                    CreatedAt = a.CreatedAt
                }).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}

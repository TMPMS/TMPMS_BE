using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPMS.Data;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly TMPMSDbContext _context;
        public AuditLogRepository(TMPMSDbContext context) => _context = context;

        public async Task<AuditLog> CreateAsync(AuditLog log)
        {
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<(List<AuditLog> Items, int TotalCount)> QueryAsync(
            int? userId, string? entityName, string? action,
            DateTime? fromDate, DateTime? toDate, int page, int pageSize)
        {
            var q = _context.AuditLogs.AsQueryable();

            if (userId.HasValue) q = q.Where(a => a.UserId == userId);
            if (!string.IsNullOrWhiteSpace(entityName)) q = q.Where(a => a.EntityName == entityName);
            if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action == action);
            if (fromDate.HasValue) q = q.Where(a => a.CreatedAt >= fromDate.Value);
            if (toDate.HasValue) q = q.Where(a => a.CreatedAt <= toDate.Value);

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}

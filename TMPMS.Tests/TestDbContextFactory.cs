using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;

namespace TMPMS.Tests
{
    // Tạo TMPMSDbContext chạy trên SQLite in-memory cho test — dùng SQLite (thay vì EF Core
    // InMemory provider) vì một số service (LoyaltyService) dùng ExecuteUpdateAsync, tính năng
    // InMemory provider không hỗ trợ. Giữ kết nối SQLite mở trong suốt vòng đời context vì
    // ":memory:" chỉ tồn tại khi có ít nhất 1 connection đang mở.
    public sealed class SqliteTestDbContext : IDisposable
    {
        private readonly SqliteConnection _connection;
        public TMPMSDbContext Context { get; }

        public SqliteTestDbContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<TMPMSDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new TMPMSDbContext(options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}

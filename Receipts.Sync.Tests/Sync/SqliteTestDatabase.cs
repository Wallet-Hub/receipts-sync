using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Receipts.Sync.Data.EfCore;

namespace Receipts.Sync.Tests.Sync
{
    /// <summary>
    /// Throwaway SQLite database created from the migrations of the production persistence package.
    /// </summary>
    internal sealed class SqliteTestDatabase : IAsyncDisposable
    {
        public const string MigrationsAssemblyName = "Receipts.Sync.Data.EfCore.Sqlite";

        private readonly string _path;

        public SqliteTestDatabase()
        {
            _path = Path.Combine(Path.GetTempPath(), "receipts-sync-tests", $"{Guid.NewGuid():N}.db");
        }

        public string ConnectionString => $"Data Source={_path}";

        public async Task InitializeAsync()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            await using ExpenseReceiptDbContext context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public ExpenseReceiptDbContext CreateContext()
        {
            DbContextOptions<ExpenseReceiptDbContext> options = new DbContextOptionsBuilder<ExpenseReceiptDbContext>()
                .UseSqlite(ConnectionString, sqlite => sqlite.MigrationsAssembly(MigrationsAssemblyName))
                .Options;

            return new ExpenseReceiptDbContext(options);
        }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            return ValueTask.CompletedTask;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Receipts.Sync.Data.EfCore.Sqlite
{
    /// <summary>
    /// Used by the EF Core tools (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>).
    /// </summary>
    public sealed class ExpenseReceiptDbContextFactory : IDesignTimeDbContextFactory<ExpenseReceiptDbContext>
    {
        public ExpenseReceiptDbContext CreateDbContext(string[] args)
        {
            string connectionString = Environment.GetEnvironmentVariable("RECEIPTS_SYNC_CONNECTION")
                ?? "Data Source=receipts-sync.db";

            DbContextOptions<ExpenseReceiptDbContext> options = new DbContextOptionsBuilder<ExpenseReceiptDbContext>()
                .UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(SqliteServiceCollectionExtensions.MigrationsAssembly))
                .Options;

            return new ExpenseReceiptDbContext(options);
        }
    }
}

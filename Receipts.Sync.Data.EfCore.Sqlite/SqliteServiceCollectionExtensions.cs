using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Receipts.Sync.Data.EfCore;

namespace Receipts.Sync.Data.EfCore.Sqlite
{
    public static class SqliteServiceCollectionExtensions
    {
        /// <summary>
        /// Name of the assembly holding the SQLite migrations of <see cref="ExpenseReceiptDbContext"/>.
        /// </summary>
        internal static readonly string MigrationsAssembly = typeof(SqliteServiceCollectionExtensions).Assembly.GetName().Name!;

        /// <summary>
        /// Registers <see cref="ExpenseReceiptDbContext"/> against a SQLite database.
        /// Swapping the database means replacing this package, not the callers.
        /// </summary>
        public static IServiceCollection AddExpenseReceiptsSqlite(this IServiceCollection services, string connectionString)
        {
            ArgumentNullException.ThrowIfNull(services);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A SQLite connection string is required.", nameof(connectionString));
            }

            services.AddDbContext<ExpenseReceiptDbContext>(options =>
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(MigrationsAssembly)));

            return services;
        }
    }
}

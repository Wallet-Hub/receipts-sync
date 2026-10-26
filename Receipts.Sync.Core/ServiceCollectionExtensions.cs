using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Receipts.Sync.Core.Sync;

namespace Receipts.Sync.Core
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="ReceiptSyncService"/>. Requires a registered
        /// <see cref="Data.EfCore.ExpenseReceiptDbContext"/> and the provider scanning infrastructure.
        /// </summary>
        public static IServiceCollection AddReceiptSync(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddScoped<ReceiptSyncService>();
            return services;
        }
    }
}

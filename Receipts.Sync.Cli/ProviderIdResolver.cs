using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Receipts.Sync.Data.EfCore;

namespace Receipts.Sync.Cli
{
    internal static class ProviderIdResolver
    {
        public static async Task<IReadOnlyCollection<int>?> ResolveAsync(
            ExpenseReceiptDbContext db,
            IReadOnlyList<string> requested,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            List<ProviderReference> available = await db.FileProviders
                .AsNoTracking()
                .Select(provider => new ProviderReference(provider.Id, provider.Name))
                .ToListAsync(cancellationToken);

            List<int> ids = new(requested.Count);

            foreach (string value in requested)
            {
                ProviderReference? match = available.FirstOrDefault(provider =>
                    string.Equals(provider.Name, value, StringComparison.OrdinalIgnoreCase))
                    ?? available.FirstOrDefault(provider => provider.Id.ToString(CultureInfo.InvariantCulture) == value);

                if (match is null)
                {
                    logger.LogError(
                        "Unknown provider '{Provider}'. Available providers: {Available}",
                        value,
                        available.Count == 0
                            ? "(none)"
                            : string.Join(", ", available.Select(provider => $"{provider.Id} ({provider.Name})")));

                    return null;
                }

                if (!ids.Contains(match.Id))
                    ids.Add(match.Id);
            }

            return ids;
        }
    }
}

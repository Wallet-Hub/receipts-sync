using Microsoft.Extensions.Logging;
using Receipts.Sync.Core.Sync;

namespace Receipts.Sync.Cli
{
    internal static class SyncSummaryLogger
    {
        public static void Log(SyncSummary summary, bool dryRun, ILogger logger)
        {
            if (dryRun)
                logger.LogInformation("Dry run: no changes were written to the database.");

            if (summary.Providers.Count == 0)
            {
                logger.LogInformation("No enabled file provider was found. Insert a row in FileProviders to start scanning.");
                return;
            }

            foreach (ProviderSyncResult provider in summary.Providers)
            {
                logger.LogInformation(
                    "{ProviderName} [{ProviderType}]: {FilesDiscovered} discovered, {FilesAdded} added, " +
                    "{FilesUpdated} updated, {FilesUnchanged} unchanged, {FilesFailed} failed",
                    provider.ProviderName,
                    provider.ProviderType,
                    provider.FilesDiscovered,
                    provider.FilesAdded,
                    provider.FilesUpdated,
                    provider.FilesUnchanged,
                    provider.FilesFailed);

                foreach (string error in provider.Errors)
                    logger.LogWarning("Provider {ProviderName}: {Error}", provider.ProviderName, error);
            }

            logger.LogInformation(
                "Total: {FilesDiscovered} discovered, {FilesAdded} added, {FilesUpdated} updated, " +
                "{FilesUnchanged} unchanged, {FilesFailed} failed.",
                summary.FilesDiscovered,
                summary.FilesAdded,
                summary.FilesUpdated,
                summary.FilesUnchanged,
                summary.FilesFailed);
        }
    }
}

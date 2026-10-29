using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Receipts.Sync.Cli.Commands;
using Receipts.Sync.Core;
using Receipts.Sync.Core.Sync;
using Receipts.Sync.Data.EfCore;
using Receipts.Sync.Data.EfCore.Sqlite;
using Receipts.Sync.Providers;
using Receipts.Sync.Providers.GoogleDrive;

namespace Receipts.Sync.Cli
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            CommandLineArguments arguments;

            try
            {
                arguments = CommandLineArguments.Parse(args);
            }
            catch (ArgumentException ex)
            {
                await Console.Error.WriteLineAsync(ex.Message);
                return 2;
            }

            if (arguments.ShowHelp)
            {
                await Console.Out.WriteLineAsync(CommandLineArguments.Usage);
                return 0;
            }

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = [],
                ContentRootPath = AppContext.BaseDirectory,
            });

            builder.Configuration
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true, reloadOnChange: false);

            string? connectionString = builder.Configuration.GetConnectionString("ReceiptsSync");
            ArgumentNullException.ThrowIfNull(connectionString, "Connection string 'ReceiptsSync' is not configured.");

            builder.Services
                .AddFileProviderScanning()
                .AddGoogleDriveFileProvider()
                .AddExpenseReceiptsSqlite(connectionString)
                .AddReceiptSync();

            using IHost host = builder.Build();
            using IServiceScope scope = host.Services.CreateScope();

            IServiceProvider services = scope.ServiceProvider;
            ILogger logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Receipts.Sync.Cli");

            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };

            try
            {
                ExpenseReceiptDbContext db = services.GetRequiredService<ExpenseReceiptDbContext>();
                await db.Database.MigrateAsync(shutdown.Token);

                IReadOnlyCollection<int>? providerIds = null;
                if (arguments.Providers.Count > 0)
                {
                    providerIds = await ResolveProviderIdsAsync(db, arguments.Providers, logger, shutdown.Token);
                    if (providerIds is null)
                    {
                        return 2;
                    }
                }

                ReceiptSyncService syncService = services.GetRequiredService<ReceiptSyncService>();
                var syncOptions = new SyncOptions { ProviderIds = providerIds, DryRun = arguments.DryRun };

                SyncSummary summary = await syncService.SyncAsync(syncOptions, shutdown.Token);

                await WriteSummaryAsync(summary, arguments.DryRun);
                return summary.HasErrors ? 1 : 0;
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Synchronization cancelled.");
                return 130;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Synchronization failed.");
                return 1;
            }
        }

        private static async Task<IReadOnlyCollection<int>?> ResolveProviderIdsAsync(ExpenseReceiptDbContext db, IReadOnlyList<string> requested, ILogger logger,
            CancellationToken cancellationToken)
        {
            List<ProviderReference> available = await db.FileProviders
                .AsNoTracking()
                .Select(p => new ProviderReference(p.Id, p.Name))
                .ToListAsync(cancellationToken);

            var ids = new List<int>(requested.Count);

            foreach (string value in requested)
            {
                ProviderReference? match = available.FirstOrDefault(p => string.Equals(p.Name, value, StringComparison.OrdinalIgnoreCase))
                    ?? available.FirstOrDefault(p => p.Id.ToString(CultureInfo.InvariantCulture) == value);

                if (match is null)
                {
                    logger.LogError("Unknown provider '{Provider}'. Available providers: {Available}", value,
                        available.Count == 0 ? "(none)" : string.Join(", ", available.Select(p => $"{p.Id} ({p.Name})")));

                    return null;
                }

                if (!ids.Contains(match.Id))
                {
                    ids.Add(match.Id);
                }
            }

            return ids;
        }

        private static async Task WriteSummaryAsync(SyncSummary summary, bool dryRun)
        {
            if (dryRun)
            {
                await Console.Out.WriteLineAsync("Dry run: no changes were written to the database.");
            }

            if (summary.Providers.Count == 0)
            {
                await Console.Out.WriteLineAsync("No enabled file provider was found. Insert a row in FileProviders to start scanning.");
                return;
            }

            foreach (ProviderSyncResult provider in summary.Providers)
            {
                await Console.Out.WriteLineAsync(
                    $"  {provider.ProviderName} [{provider.ProviderType}]: " +
                    $"{provider.FilesDiscovered} discovered, {provider.FilesAdded} added, {provider.FilesUpdated} updated, " +
                    $"{provider.FilesUnchanged} unchanged, {provider.FilesFailed} failed");

                foreach (string error in provider.Errors)
                {
                    await Console.Out.WriteLineAsync($"    - {error}");
                }
            }

            await Console.Out.WriteLineAsync(
                $"Total: {summary.FilesDiscovered} discovered, {summary.FilesAdded} added, {summary.FilesUpdated} updated, " +
                $"{summary.FilesUnchanged} unchanged, {summary.FilesFailed} failed.");
        }
    }

    internal sealed record ProviderReference(int Id, string Name);
}
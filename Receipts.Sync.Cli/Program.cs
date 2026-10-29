using System;
using System.Collections.Generic;
using System.IO;
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
            using ILoggerFactory startupLoggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
            ILogger startupLogger = startupLoggerFactory.CreateLogger("Receipts.Sync.Cli");

            CommandLineArguments arguments;

            try
            {
                arguments = CommandLineArguments.Parse(args);
            }
            catch (ArgumentException ex)
            {
                startupLogger.LogError("Invalid command-line arguments: {Error}", ex.Message);
                return ExitCodes.InvalidArguments;
            }

            if (arguments.ShowHelp)
            {
                startupLogger.LogInformation("{Usage}", CommandLineArguments.Usage);
                return ExitCodes.Success;
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
                    providerIds = await ProviderIdResolver.ResolveAsync(db, arguments.Providers, logger, shutdown.Token);
                    if (providerIds is null)
                        return ExitCodes.InvalidArguments;
                }

                ReceiptSyncService syncService = services.GetRequiredService<ReceiptSyncService>();
                var syncOptions = new SyncOptions
                {
                    ProviderIds = providerIds,
                    DryRun = arguments.DryRun
                };

                SyncSummary summary = await syncService.SyncAsync(syncOptions, shutdown.Token);

                SyncSummaryLogger.Log(summary, arguments.DryRun, logger);
                return summary.HasErrors ? ExitCodes.Failure : ExitCodes.Success;
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Synchronization cancelled.");

                return ExitCodes.Cancelled;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Synchronization failed.");

                return ExitCodes.Failure;
            }
        }
    }
}
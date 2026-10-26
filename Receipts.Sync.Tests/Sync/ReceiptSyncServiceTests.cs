using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Receipts.Sync.Core.Sync;
using Receipts.Sync.Data.EfCore;
using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers;
using Receipts.Sync.Providers.Abstractions;
using Receipts.Sync.Providers.Hashing;

namespace Receipts.Sync.Tests.Sync
{
    public sealed class ReceiptSyncServiceTests
    {
        private static readonly DateTime CreatedAt = new(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ModifiedAt = new(2026, 1, 11, 9, 30, 0, DateTimeKind.Utc);

        [Fact]
        public async Task SyncAsync_InsertsReceiptsWithFileMetadata()
        {
            await using SqliteTestDatabase database = await CreateDatabaseAsync();
            FakeFileProviderSource source = new FakeFileProviderSource().Add("2026/invoice.pdf", "content-a", CreatedAt, ModifiedAt);
            int providerId = await SeedProviderAsync(database, "local");

            SyncSummary summary = await RunAsync(database, null, source);

            Assert.Equal(1, summary.FilesDiscovered);
            Assert.Equal(1, summary.FilesAdded);
            Assert.Equal(0, summary.FilesFailed);
            Assert.False(summary.HasErrors);

            await using ExpenseReceiptDbContext db = database.CreateContext();
            ExpenseFileReceipt receipt = await db.ExpenseFileReceipts.SingleAsync();

            Assert.Equal(providerId, receipt.FileProviderId);
            Assert.Equal("2026/invoice.pdf", receipt.ExternalId);
            Assert.Equal("invoice.pdf", receipt.FileName);
            Assert.Equal("application/pdf", receipt.ContentType);
            Assert.Equal(Encoding.UTF8.GetByteCount("content-a"), receipt.SizeInBytes);
            Assert.Equal("SHA256", receipt.HashAlgorithm);
            Assert.Equal(ExpectedHash("content-a"), receipt.Hash);
            Assert.Equal(CreatedAt, receipt.CreatedAt);
            Assert.Equal(ModifiedAt, receipt.UpdatedAt);
            Assert.Null(receipt.Url);
        }

        [Fact]
        public async Task SyncAsync_IsIdempotentWhenFilesDidNotChange()
        {
            await using SqliteTestDatabase database = await CreateDatabaseAsync();
            FakeFileProviderSource source = new FakeFileProviderSource().Add("2026/invoice.pdf", "content-a", CreatedAt, ModifiedAt);
            await SeedProviderAsync(database, "local");

            await RunAsync(database, null, source);
            int opensAfterFirstRun = source.OpenReadCount;
            SyncSummary summary = await RunAsync(database, null, source);

            Assert.Equal(1, summary.FilesUnchanged);
            Assert.Equal(0, summary.FilesAdded);
            Assert.Equal(0, summary.FilesUpdated);
            Assert.Equal(opensAfterFirstRun, source.OpenReadCount);

            await using ExpenseReceiptDbContext db = database.CreateContext();
            Assert.Equal(1, await db.ExpenseFileReceipts.CountAsync());
        }

        [Fact]
        public async Task SyncAsync_UpdatesReceiptAndHashWhenContentChanges()
        {
            await using SqliteTestDatabase database = await CreateDatabaseAsync();
            FakeFileProviderSource source = new FakeFileProviderSource().Add("2026/invoice.pdf", "content-a", CreatedAt, ModifiedAt);
            await SeedProviderAsync(database, "local");

            await RunAsync(database, null, source);

            DateTime updatedFile = ModifiedAt.AddMinutes(5);
            source.Add("2026/invoice.pdf", "content-b", CreatedAt, updatedFile);
            SyncSummary summary = await RunAsync(database, null, source);

            Assert.Equal(1, summary.FilesUpdated);
            Assert.Equal(0, summary.FilesAdded);
            Assert.Equal(0, summary.FilesUnchanged);

            await using ExpenseReceiptDbContext db = database.CreateContext();
            ExpenseFileReceipt receipt = await db.ExpenseFileReceipts.SingleAsync();

            Assert.Equal(ExpectedHash("content-b"), receipt.Hash);
            Assert.Equal(updatedFile, receipt.UpdatedAt);
        }

        [Fact]
        public async Task SyncAsync_DryRunDoesNotWriteAnything()
        {
            await using SqliteTestDatabase database = await CreateDatabaseAsync();
            FakeFileProviderSource source = new FakeFileProviderSource().Add("2026/invoice.pdf", "content-a", CreatedAt, ModifiedAt);
            await SeedProviderAsync(database, "local");

            SyncSummary summary = await RunAsync(database, new SyncOptions { DryRun = true }, source);

            Assert.Equal(1, summary.FilesDiscovered);
            Assert.Equal(1, summary.FilesAdded);
            Assert.Equal(0, source.OpenReadCount);

            await using ExpenseReceiptDbContext db = database.CreateContext();
            Assert.Equal(0, await db.ExpenseFileReceipts.CountAsync());
        }

        [Fact]
        public async Task SyncAsync_SkipsDisabledProviders()
        {
            await using SqliteTestDatabase database = await CreateDatabaseAsync();
            FakeFileProviderSource source = new FakeFileProviderSource().Add("2026/invoice.pdf", "content-a", CreatedAt, ModifiedAt);
            await SeedProviderAsync(database, "disabled", isEnabled: false);

            SyncSummary summary = await RunAsync(database, null, source);

            Assert.Empty(summary.Providers);
            Assert.Equal(0, summary.FilesDiscovered);
        }

        [Fact]
        public async Task SyncAsync_ScansOnlyTheSelectedProviders()
        {
            await using SqliteTestDatabase database = await CreateDatabaseAsync();
            FakeFileProviderSource source = new FakeFileProviderSource().Add("2026/invoice.pdf", "content-a", CreatedAt, ModifiedAt);
            int selectedId = await SeedProviderAsync(database, "selected");
            await SeedProviderAsync(database, "other");

            SyncSummary summary = await RunAsync(database, new SyncOptions { ProviderIds = [selectedId] }, source);

            ProviderSyncResult provider = Assert.Single(summary.Providers);
            Assert.Equal(selectedId, provider.ProviderId);

            await using ExpenseReceiptDbContext db = database.CreateContext();
            ExpenseFileReceipt receipt = await db.ExpenseFileReceipts.SingleAsync();
            Assert.Equal(selectedId, receipt.FileProviderId);
        }

        [Fact]
        public async Task SyncAsync_RecordsFailureWhenNoSourceHandlesTheProviderType()
        {
            await using SqliteTestDatabase database = await CreateDatabaseAsync();
            FakeFileProviderSource source = new FakeFileProviderSource(FileProviderType.GoogleDrive);
            await SeedProviderAsync(database, "azure", FileProviderType.AzureBlobStorage);

            SyncSummary summary = await RunAsync(database, null, source);

            ProviderSyncResult provider = Assert.Single(summary.Providers);
            Assert.False(provider.Succeeded);
            Assert.Equal(1, provider.FilesFailed);
            Assert.Contains("AzureBlobStorage", provider.Errors[0], StringComparison.Ordinal);
            Assert.True(summary.HasErrors);
        }

        private static async Task<SqliteTestDatabase> CreateDatabaseAsync()
        {
            var database = new SqliteTestDatabase();
            await database.InitializeAsync();
            return database;
        }

        private static async Task<int> SeedProviderAsync(
            SqliteTestDatabase database,
            string name,
            FileProviderType providerType = FileProviderType.PhysicalFolder,
            bool isEnabled = true)
        {
            await using ExpenseReceiptDbContext db = database.CreateContext();

            var provider = new FileProvider
            {
                Name = name,
                ProviderType = providerType,
                Configuration = "{}",
                IsEnabled = isEnabled,
                CreatedAt = CreatedAt,
                UpdatedAt = ModifiedAt,
            };

            db.FileProviders.Add(provider);
            await db.SaveChangesAsync();

            return provider.Id;
        }

        private static async Task<SyncSummary> RunAsync(
            SqliteTestDatabase database,
            SyncOptions? options,
            params IFileProviderSource[] sources)
        {
            await using ExpenseReceiptDbContext db = database.CreateContext();
            var service = new ReceiptSyncService(
                db,
                new FileProviderSourceRegistry(sources),
                new Sha256FileHasher(),
                NullLogger<ReceiptSyncService>.Instance);

            return await service.SyncAsync(options);
        }

        private static string ExpectedHash(string content) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}

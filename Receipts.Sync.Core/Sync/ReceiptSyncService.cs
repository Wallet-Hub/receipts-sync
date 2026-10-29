using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Receipts.Sync.Data.EfCore;
using Receipts.Sync.Data.Entities;
using Receipts.Sync.Providers.Abstractions;
using Receipts.Sync.Providers.Hashing;

namespace Receipts.Sync.Core.Sync
{
    /// <summary>
    /// Scans the enabled <see cref="FileProvider"/> rows and keeps <see cref="ExpenseFileReceipt"/> in sync
    /// with the files found on them: new files are inserted, changed files are updated and unchanged files
    /// are left untouched. Removed files are intentionally not handled yet.
    /// </summary>
    public sealed class ReceiptSyncService
    {
        private const int SaveBatchSize = 100;

        private readonly ExpenseReceiptDbContext _db;
        private readonly IFileProviderSourceRegistry _sources;
        private readonly IFileHasher _hasher;
        private readonly ILogger<ReceiptSyncService> _logger;

        public ReceiptSyncService(ExpenseReceiptDbContext db, IFileProviderSourceRegistry sources, IFileHasher hasher, ILogger<ReceiptSyncService> logger)
        {
            _db = db;
            _sources = sources;
            _hasher = hasher;
            _logger = logger;
        }

        /// <summary>
        /// Scans the providers selected by <paramref name="options"/> and records the files found on them.
        /// </summary>
        public async Task<SyncSummary> SyncAsync(SyncOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new SyncOptions();

            List<FileProvider> providers = await GetProvidersAsync(options, cancellationToken);
            var results = new List<ProviderSyncResult>(providers.Count);

            if (providers.Count == 0)
            {
                _logger.LogWarning("No enabled file provider matches the request: nothing to scan.");
                return new SyncSummary(results);
            }

            foreach (FileProvider provider in providers)
            {
                var result = new ProviderSyncResult(provider.Id, provider.Name, provider.ProviderType);
                results.Add(result);

                if (!_sources.TryGetSource(provider.ProviderType, out IFileProviderSource? source))
                {
                    result.RecordFailure($"No file provider source is registered for '{provider.ProviderType}'.");

                    _logger.LogError("No file provider source is registered for '{ProviderType}' (provider '{ProviderName}').", provider.ProviderType, provider.Name);
                    continue;
                }

                _logger.LogInformation("Scanning {ProviderType} provider '{ProviderName}'...", provider.ProviderType, provider.Name);

                try
                {
                    ProviderContext providerContext = provider.ToProviderContext();

                    await SyncProviderAsync(providerContext, source, options, result, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.RecordFailure(ex.Message);
                    _logger.LogError(ex, "Provider '{ProviderName}' could not be scanned.", provider.Name);
                }

                _logger.LogInformation(
                    "Provider '{ProviderName}': {FilesDiscovered} discovered, {FilesAdded} added, {FilesUpdated} updated, {FilesUnchanged} unchanged, {FilesFailed} failed.",
                    provider.Name,
                    result.FilesDiscovered,
                    result.FilesAdded,
                    result.FilesUpdated,
                    result.FilesUnchanged,
                    result.FilesFailed);

                // Pending changes of a failed provider are dropped so the next run starts from a clean state.
                _db.ChangeTracker.Clear();
            }

            return new SyncSummary(results);
        }

        private async Task<List<FileProvider>> GetProvidersAsync(SyncOptions options, CancellationToken cancellationToken)
        {
            IQueryable<FileProvider> query = _db.FileProviders.Where(p => p.IsEnabled);

            if (options.ProviderIds is { Count: > 0 })
            {
                int[] providerIds = options.ProviderIds.Distinct().ToArray();
                query = query.Where(p => providerIds.Contains(p.Id));
            }

            return await query
                .OrderBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        private async Task SyncProviderAsync(ProviderContext provider, IFileProviderSource source, SyncOptions options, ProviderSyncResult result, CancellationToken cancellationToken)
        {
            Dictionary<string, ExpenseFileReceipt> knownReceipts = await _db.ExpenseFileReceipts
                .Where(r => r.FileProviderId == provider.Id)
                .ToDictionaryAsync(r => r.ExternalId, StringComparer.Ordinal, cancellationToken);

            int pendingChanges = 0;

            await foreach (ProviderFileInfo file in source.EnumerateFilesAsync(provider, cancellationToken).WithCancellation(cancellationToken))
            {
                result.RecordDiscovered();

                try
                {
                    await UpsertAsync(provider, source, file, knownReceipts, options, result, cancellationToken);

                    if (!options.DryRun && ++pendingChanges >= SaveBatchSize)
                    {
                        await _db.SaveChangesAsync(cancellationToken);
                        pendingChanges = 0;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.RecordFailure($"{file.ExternalId}: {ex.Message}");
                    _logger.LogWarning(ex, "File '{ExternalId}' of provider '{ProviderName}' could not be processed.", file.ExternalId, provider.Name);
                }
            }

            if (!options.DryRun)
            {
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task UpsertAsync(ProviderContext provider, IFileProviderSource source, ProviderFileInfo file, Dictionary<string, ExpenseFileReceipt> knownReceipts,
            SyncOptions options, ProviderSyncResult result, CancellationToken cancellationToken)
        {
            DateTime createdAt = file.CreatedAt ?? file.UpdatedAt ?? DateTime.UtcNow;
            DateTime updatedAt = file.UpdatedAt ?? createdAt;

            if (!knownReceipts.TryGetValue(file.ExternalId, out ExpenseFileReceipt? receipt))
            {
                result.RecordAdded();

                if (options.DryRun)
                {
                    return;
                }

                string hash = await ComputeHashAsync(source, provider, file, cancellationToken).ConfigureAwait(false);

                receipt = new ExpenseFileReceipt
                {
                    FileProviderId = provider.Id,
                    ExternalId = file.ExternalId,
                };

                ApplyMetadata(receipt, file, hash, createdAt, updatedAt);
                _db.ExpenseFileReceipts.Add(receipt);
                knownReceipts[receipt.ExternalId] = receipt;
                return;
            }

            bool hashUnchanged = CanReuseHash(receipt, file);
            string currentHash = hashUnchanged
                ? receipt.Hash
                : await ComputeHashAsync(source, provider, file, cancellationToken).ConfigureAwait(false);

            bool changed = !string.Equals(receipt.Hash, currentHash, StringComparison.Ordinal)
                || !string.Equals(receipt.HashAlgorithm, _hasher.AlgorithmName, StringComparison.Ordinal)
                || !string.Equals(receipt.FileName, file.FileName, StringComparison.Ordinal)
                || !string.Equals(receipt.ContentType, file.ContentType, StringComparison.Ordinal)
                || receipt.SizeInBytes != file.SizeInBytes
                || !Equals(receipt.Url, file.Url)
                || receipt.CreatedAt != createdAt
                || receipt.UpdatedAt != updatedAt;

            if (!changed)
            {
                result.RecordUnchanged();
                return;
            }

            result.RecordUpdated();

            if (options.DryRun)
            {
                return;
            }

            ApplyMetadata(receipt, file, currentHash, createdAt, updatedAt);
        }

        private void ApplyMetadata(ExpenseFileReceipt receipt, ProviderFileInfo file, string hash, DateTime createdAt, DateTime updatedAt)
        {
            receipt.FileName = file.FileName;
            receipt.ContentType = file.ContentType;
            receipt.SizeInBytes = file.SizeInBytes;
            receipt.Url = file.Url;
            receipt.Hash = hash;
            receipt.HashAlgorithm = _hasher.AlgorithmName;
            receipt.CreatedAt = createdAt;
            receipt.UpdatedAt = updatedAt;
        }

        private bool CanReuseHash(ExpenseFileReceipt receipt, ProviderFileInfo file)
        {
            if (string.IsNullOrEmpty(receipt.Hash))
            {
                return false;
            }

            if (!string.Equals(receipt.HashAlgorithm, _hasher.AlgorithmName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // The provider reports neither a size nor a timestamp: there is no cheap way to know
            // whether the content changed, so it is hashed again.
            if (file.SizeInBytes is not { } size || receipt.SizeInBytes != size)
            {
                return false;
            }

            if (file.UpdatedAt is not { } updatedAt)
            {
                return false;
            }

            return Math.Abs((receipt.UpdatedAt - updatedAt).TotalSeconds) < 1;
        }

        private async Task<string> ComputeHashAsync(IFileProviderSource source, ProviderContext provider, ProviderFileInfo file, CancellationToken cancellationToken)
        {
            await using Stream content = await source.OpenReadAsync(provider, file, cancellationToken);
            return await _hasher.ComputeHashAsync(content, cancellationToken);
        }
    }
}

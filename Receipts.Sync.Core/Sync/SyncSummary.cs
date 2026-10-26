namespace Receipts.Sync.Core.Sync
{
    /// <summary>
    /// Aggregated outcome of a synchronization run.
    /// </summary>
    public sealed class SyncSummary
    {
        private readonly List<ProviderSyncResult> _providers;

        public SyncSummary(IEnumerable<ProviderSyncResult> providers)
        {
            ArgumentNullException.ThrowIfNull(providers);
            _providers = [.. providers];
        }

        public IReadOnlyList<ProviderSyncResult> Providers => _providers;

        public int FilesDiscovered => _providers.Sum(p => p.FilesDiscovered);

        public int FilesAdded => _providers.Sum(p => p.FilesAdded);

        public int FilesUpdated => _providers.Sum(p => p.FilesUpdated);

        public int FilesUnchanged => _providers.Sum(p => p.FilesUnchanged);

        public int FilesFailed => _providers.Sum(p => p.FilesFailed);

        public bool HasErrors => _providers.Any(p => !p.Succeeded);
    }
}

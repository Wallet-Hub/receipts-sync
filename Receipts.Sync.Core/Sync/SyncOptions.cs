namespace Receipts.Sync.Core.Sync
{
    /// <summary>
    /// Options of a single synchronization run.
    /// </summary>
    public sealed class SyncOptions
    {
        /// <summary>
        /// When set, only the providers with these ids are scanned. Otherwise every enabled provider is scanned.
        /// </summary>
        public IReadOnlyCollection<int>? ProviderIds { get; init; }

        /// <summary>
        /// When <c>true</c> the providers are scanned and hashes are computed, but nothing is written to the database.
        /// </summary>
        public bool DryRun { get; init; }
    }
}

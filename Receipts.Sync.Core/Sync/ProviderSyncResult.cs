using Receipts.Sync.Data.Entities;

namespace Receipts.Sync.Core.Sync
{
    /// <summary>
    /// Outcome of the scan of a single <see cref="FileProvider"/>.
    /// </summary>
    public sealed class ProviderSyncResult
    {
        private readonly List<string> _errors = [];

        public ProviderSyncResult(int providerId, string providerName, FileProviderType providerType)
        {
            ProviderId = providerId;
            ProviderName = providerName;
            ProviderType = providerType;
        }

        public int ProviderId { get; }

        public string ProviderName { get; }

        public FileProviderType ProviderType { get; }

        /// <summary>
        /// Files returned by the provider source.
        /// </summary>
        public int FilesDiscovered { get; private set; }

        /// <summary>
        /// Files inserted as new <see cref="ExpenseFileReceipt"/> rows (or that would be, on a dry run).
        /// </summary>
        public int FilesAdded { get; private set; }

        /// <summary>
        /// Existing receipts whose metadata or hash changed (or that would change, on a dry run).
        /// </summary>
        public int FilesUpdated { get; private set; }

        /// <summary>
        /// Existing receipts that already matched the file found on the provider.
        /// </summary>
        public int FilesUnchanged { get; private set; }

        public int FilesFailed { get; private set; }

        public IReadOnlyList<string> Errors => _errors;

        public bool Succeeded => _errors.Count == 0;

        internal void RecordDiscovered() => FilesDiscovered++;

        internal void RecordAdded() => FilesAdded++;

        internal void RecordUpdated() => FilesUpdated++;

        internal void RecordUnchanged() => FilesUnchanged++;

        internal void RecordFailure(string error)
        {
            FilesFailed++;
            _errors.Add(error);
        }
    }
}

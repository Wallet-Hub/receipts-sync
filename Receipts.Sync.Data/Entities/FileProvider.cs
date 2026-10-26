using System;

namespace Receipts.Sync.Data.Entities
{
    /// <summary>
    /// A source of receipt files (local folder, Google Drive, Azure Blob Storage, ...).
    /// </summary>
    public class FileProvider
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public FileProviderType ProviderType { get; set; }

        /// <summary>
        /// Provider-specific settings serialized as JSON (e.g. root path, drive and folder id, container name).
        /// Credentials must not be stored here: reference them through the secret store instead.
        /// </summary>
        public string Configuration { get; set; } = null!;

        public bool IsEnabled { get; set; } = true;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public IList<ExpenseFileReceipt> FileReceipts { get; set; } = [];
    }
}

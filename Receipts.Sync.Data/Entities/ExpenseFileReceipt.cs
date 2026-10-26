using System;

namespace Receipts.Sync.Data.Entities
{
    /// <summary>
    /// A single receipt file discovered on a <see cref="FileProvider"/>.
    /// </summary>
    public class ExpenseFileReceipt
    {
        public int Id { get; set; }
        public int FileProviderId { get; set; }
        public FileProvider? Provider { get; set; }

        /// <summary>
        /// Identifier of the file within the provider (relative path, drive file id, blob name).
        /// Unique together with <see cref="FileProviderId"/>.
        /// </summary>
        public string ExternalId { get; set; } = null!;

        /// <summary>
        /// Content hash (SHA-256 by default), stored as a lowercase hexadecimal string.
        /// Used for deduplication and change detection.
        /// </summary>
        public string Hash { get; set; } = null!;

        public string HashAlgorithm { get; set; } = "SHA256";

        /// <summary>
        /// Location of the file. Null when the provider does not expose one (e.g. a local folder).
        /// </summary>
        public Uri? Url { get; set; }

        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long? SizeInBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ExpenseReceiptExtraction? Extraction { get; set; }
    }
}

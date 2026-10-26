using System;

namespace Receipts.Sync.Data.Entities
{
    /// <summary>
    /// Metadata extracted from a <see cref="ExpenseFileReceipt"/>. At most one row per file:
    /// a new extraction run overwrites the previous one.
    /// </summary>
    public class ExpenseReceiptExtraction
    {
        public int Id { get; set; }
        public int ExpenseFileReceiptId { get; set; }
        public ExpenseFileReceipt? FileReceipt { get; set; }

        /// <summary>
        /// Structured metadata serialized as JSON (merchant, amount, currency, date, line items, ...).
        /// </summary>
        public string Metadata { get; set; } = null!;

        /// <summary>
        /// Optional raw output of the extractor (OCR text, provider response) kept for reprocessing.
        /// </summary>
        public string? RawResponse { get; set; }

        public string? ExtractorName { get; set; }
        public DateTime ExtractedAt { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Receipts.Sync.Data.Entities;

namespace Receipts.Sync.Data.EfCore.Configurations
{
    public class ExpenseReceiptExtractionConfiguration : IEntityTypeConfiguration<ExpenseReceiptExtraction>
    {
        public void Configure(EntityTypeBuilder<ExpenseReceiptExtraction> builder)
        {
            builder.ToTable("ExpenseReceiptExtractions");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Metadata)
                .IsRequired();

            builder.Property(e => e.ExtractorName)
                .HasMaxLength(128);

            builder.Property(e => e.ExtractedAt)
                .IsRequired();

            // One-to-one: a receipt file has at most one extraction, which is replaced on re-extraction.
            builder.HasIndex(e => e.ExpenseFileReceiptId)
                .IsUnique();

            builder.HasOne(e => e.FileReceipt)
                .WithOne(r => r.Extraction)
                .HasForeignKey<ExpenseReceiptExtraction>(e => e.ExpenseFileReceiptId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

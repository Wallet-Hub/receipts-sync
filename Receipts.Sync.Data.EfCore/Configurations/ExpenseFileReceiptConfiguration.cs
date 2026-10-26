using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Receipts.Sync.Data.Entities;

namespace Receipts.Sync.Data.EfCore.Configurations
{
    public class ExpenseFileReceiptConfiguration : IEntityTypeConfiguration<ExpenseFileReceipt>
    {
        public void Configure(EntityTypeBuilder<ExpenseFileReceipt> builder)
        {
            builder.ToTable("ExpenseFileReceipts");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.ExternalId)
                .IsRequired()
                .HasMaxLength(1024);

            builder.Property(r => r.Hash)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(r => r.HashAlgorithm)
                .IsRequired()
                .HasMaxLength(32)
                .HasDefaultValue("SHA256");

            builder.Property(r => r.Url)
                .HasMaxLength(2048)
                .HasConversion(
                    url => url!.ToString(),
                    value => new Uri(value));

            builder.Property(r => r.FileName)
                .HasMaxLength(512);

            builder.Property(r => r.ContentType)
                .HasMaxLength(128);

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired();

            builder.HasIndex(r => new { r.FileProviderId, r.ExternalId })
                .IsUnique();

            builder.HasIndex(r => r.Hash);

            builder.HasOne(r => r.Provider)
                .WithMany(p => p.FileReceipts)
                .HasForeignKey(r => r.FileProviderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

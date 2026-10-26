using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Receipts.Sync.Data.Entities;

namespace Receipts.Sync.Data.EfCore.Configurations
{
    public class FileProviderConfiguration : IEntityTypeConfiguration<FileProvider>
    {
        public void Configure(EntityTypeBuilder<FileProvider> builder)
        {
            builder.ToTable("FileProviders");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(p => p.Name)
                .IsUnique();

            builder.Property(p => p.ProviderType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(64);

            builder.Property(p => p.Configuration)
                .IsRequired();

            builder.Property(p => p.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(p => p.Description)
                .HasMaxLength(1024);

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired();
        }
    }
}

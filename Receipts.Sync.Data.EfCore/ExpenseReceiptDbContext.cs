using Microsoft.EntityFrameworkCore;
using Receipts.Sync.Data.EfCore.Configurations;
using Receipts.Sync.Data.Entities;

namespace Receipts.Sync.Data.EfCore
{
    /// <summary>
    /// Context for receipt file ingestion: file providers, receipt files and the metadata
    /// extracted from them.
    /// </summary>
    public class ExpenseReceiptDbContext : DbContext
    {
        public ExpenseReceiptDbContext(DbContextOptions<ExpenseReceiptDbContext> options)
            : base(options)
        {
        }

        public DbSet<FileProvider> FileProviders => Set<FileProvider>();
        public DbSet<ExpenseFileReceipt> ExpenseFileReceipts => Set<ExpenseFileReceipt>();
        public DbSet<ExpenseReceiptExtraction> ExpenseReceiptExtractions => Set<ExpenseReceiptExtraction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new FileProviderConfiguration());
            modelBuilder.ApplyConfiguration(new ExpenseFileReceiptConfiguration());
            modelBuilder.ApplyConfiguration(new ExpenseReceiptExtractionConfiguration());
        }
    }
}

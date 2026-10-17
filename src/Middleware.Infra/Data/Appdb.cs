using Middleware.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Middleware.Infra.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options)
    : base (options)
    {
    }
    public DbSet<Invoice> Invoice { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(b =>
        {
            b.HasKey(i => i.Id);
            b.HasIndex(i => new { i.StoreCode, i.PosTerminalId, i.InvoiceNumber }).IsUnique();
            b.Property(i => i.Status).HasConversion<string>();

            // Value objects: stored as columns on the Invoice table
            b.OwnsOne(i => i.Seller);
            b.OwnsOne(i => i.Customer);
            b.OwnsOne(i => i.Totals);
            b.OwnsOne(i => i.Payment);

            // Line items: stored in their own table, filled through the _lines backing field
            b.OwnsMany(i => i.Lines, l =>
            {
                l.ToTable("InvoiceLines");
                l.WithOwner().HasForeignKey("InvoiceId");
                l.Property<int>("Id");   // shadow key: SQLite can't autogenerate composite keys
                l.HasKey("Id");
                l.Property(x => x.TaxType).HasConversion<string>();
            });
            b.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }

}

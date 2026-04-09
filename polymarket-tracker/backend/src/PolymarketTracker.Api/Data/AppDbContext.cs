using Microsoft.EntityFrameworkCore;
using PolymarketTracker.Api.Models.Domain;

namespace PolymarketTracker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Market> Markets => Set<Market>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();
    public DbSet<Issue> Issues => Set<Issue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Markets
        modelBuilder.Entity<Market>(e =>
        {
            e.HasIndex(m => m.ConditionId).IsUnique();
            e.Property(m => m.Volume).HasPrecision(18, 2);
            e.Property(m => m.Liquidity).HasPrecision(18, 2);
            e.Property(m => m.CurrentPrice).HasPrecision(10, 6);
        });

        // PriceSnapshots
        modelBuilder.Entity<PriceSnapshot>(e =>
        {
            e.HasIndex(p => new { p.MarketId, p.Timestamp });
            e.Property(p => p.Price).HasPrecision(10, 6);
            e.HasOne(p => p.Market)
                .WithMany(m => m.PriceSnapshots)
                .HasForeignKey(p => p.MarketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Trades
        modelBuilder.Entity<Trade>(e =>
        {
            e.HasIndex(t => t.PolymarketTradeId).IsUnique();
            e.HasIndex(t => new { t.MarketId, t.Timestamp })
                .IsDescending(false, true);
            e.Property(t => t.Price).HasPrecision(10, 6);
            e.Property(t => t.Size).HasPrecision(18, 6);
            e.HasOne(t => t.Market)
                .WithMany(m => m.Trades)
                .HasForeignKey(t => t.MarketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NewsItems
        modelBuilder.Entity<NewsItem>(e =>
        {
            e.HasIndex(n => n.Url).IsUnique();
            e.Property(n => n.Tone).HasPrecision(8, 4);
            e.HasOne(n => n.Market)
                .WithMany(m => m.NewsItems)
                .HasForeignKey(n => n.MarketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Issues
        modelBuilder.Entity<Issue>(e =>
        {
            e.HasIndex(i => i.IssueNumber).IsUnique();
            e.Property(i => i.IssueNumber)
                .UseIdentityAlwaysColumn();
        });
    }
}

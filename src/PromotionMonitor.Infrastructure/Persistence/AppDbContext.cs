using Microsoft.EntityFrameworkCore;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Entities;

namespace PromotionMonitor.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Monitoring> Monitorings => Set<Monitoring>();
    public DbSet<PriceObservation> Observations => Set<PriceObservation>();
    public DbSet<PriceCalculation> Calculations => Set<PriceCalculation>();
    public DbSet<NotificationDelivery> Deliveries => Set<NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
            e.Property(x => x.NotificationEmail).HasMaxLength(256);
        });

        modelBuilder.Entity<Monitoring>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.IsActive });
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ProductUrl).HasMaxLength(2048).IsRequired();
            e.Property(x => x.ModelName).HasMaxLength(200);
            e.Property(x => x.Cep).HasMaxLength(16);
            e.Property(x => x.TargetPrice).HasPrecision(18, 2);
            e.Property(x => x.MaxPrice).HasPrecision(18, 2);
            e.HasOne(x => x.User).WithMany(u => u.Monitorings).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PriceObservation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MonitoringId, x.ObservedAt });
            e.HasIndex(x => new { x.MonitoringId, x.Fingerprint });
            e.Property(x => x.ListedPrice).HasPrecision(18, 2);
            e.Property(x => x.ShippingEstimate).HasPrecision(18, 2);
            e.Property(x => x.CouponDiscount).HasPrecision(18, 2);
            e.Property(x => x.CashbackEstimate).HasPrecision(18, 2);
            e.Property(x => x.SourceName).HasMaxLength(100);
            e.Property(x => x.Currency).HasMaxLength(8);
            e.Property(x => x.Fingerprint).HasMaxLength(128);
            e.HasOne(x => x.Monitoring).WithMany(m => m.Observations).HasForeignKey(x => x.MonitoringId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Calculation).WithOne(c => c.Observation)
                .HasForeignKey<PriceCalculation>(c => c.ObservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PriceCalculation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ListedPrice).HasPrecision(18, 2);
            e.Property(x => x.Shipping).HasPrecision(18, 2);
            e.Property(x => x.CouponDiscount).HasPrecision(18, 2);
            e.Property(x => x.PayAtCheckout).HasPrecision(18, 2);
            e.Property(x => x.CashbackEstimate).HasPrecision(18, 2);
            e.Property(x => x.NetAfterCashback).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(1024);
        });

        modelBuilder.Entity<NotificationDelivery>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(300);
            e.Property(x => x.Body).HasMaxLength(4000);
            e.HasOne(x => x.User).WithMany(u => u.Deliveries).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Monitoring).WithMany(m => m.Deliveries).HasForeignKey(x => x.MonitoringId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

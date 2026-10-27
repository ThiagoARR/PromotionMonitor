using Microsoft.EntityFrameworkCore;
using PromotionMonitor.Domain.Entities;

namespace PromotionMonitor.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<UserAccount> Users { get; }
    DbSet<Monitoring> Monitorings { get; }
    DbSet<PriceObservation> Observations { get; }
    DbSet<PriceCalculation> Calculations { get; }
    DbSet<NotificationDelivery> Deliveries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

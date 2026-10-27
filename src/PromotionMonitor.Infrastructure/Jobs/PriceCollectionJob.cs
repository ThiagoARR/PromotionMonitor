using PromotionMonitor.Application.Services;

namespace PromotionMonitor.Infrastructure.Jobs;

public sealed class PriceCollectionJob
{
    private readonly IMonitoringCollector _collector;

    public PriceCollectionJob(IMonitoringCollector collector)
    {
        _collector = collector;
    }

    public Task CollectDueAsync() => _collector.CollectDueAsync();

    public Task CollectOneAsync(Guid monitoringId) => _collector.CollectAsync(monitoringId);
}

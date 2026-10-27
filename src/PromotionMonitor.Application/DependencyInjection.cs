using Microsoft.Extensions.DependencyInjection;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Application.Services;

namespace PromotionMonitor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IEffectiveCostCalculator, EffectiveCostCalculator>();
        services.AddSingleton<IUrlSafetyValidator, UrlSafetyValidator>();
        services.AddScoped<ICouponEvaluator, CouponEvaluator>();
        services.AddScoped<ICashbackEvaluator, CashbackEvaluator>();
        services.AddScoped<IMonitoringCollector, MonitoringCollector>();
        services.AddScoped<IAlertDispatcher, AlertDispatcher>();
        return services;
    }
}

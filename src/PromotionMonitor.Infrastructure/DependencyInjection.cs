using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromotionMonitor.Application.Abstractions;
using PromotionMonitor.Domain.Enums;
using PromotionMonitor.Infrastructure.Auth;
using PromotionMonitor.Infrastructure.Jobs;
using PromotionMonitor.Infrastructure.Notifications;
using PromotionMonitor.Infrastructure.Persistence;
using PromotionMonitor.Infrastructure.Sources;

namespace PromotionMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is required.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHttpClient("PriceFetch", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PromotionMonitor/1.0 (+local-dev; respectful-fetcher)");
        });

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPriceSource, MockCatalogSource>();
        services.AddScoped<IPriceSource, HtmlStructuredSource>();
        services.AddScoped<INotificationProvider, ConsoleNotificationProvider>();
        services.AddScoped<INotificationProvider, EmailNotificationProvider>();
        // WhatsApp / WebPush intentionally not registered for delivery in Phase 1.

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(15),
                UseRecommendedIsolationLevel = true,
                PrepareSchemaIfNecessary = true
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1;
            options.SchedulePollingInterval = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<PriceCollectionJob>();

        return services;
    }

    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<PriceCollectionJob>(
            "collect-due-monitorings",
            job => job.CollectDueAsync(),
            Cron.Minutely);
    }
}

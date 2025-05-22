using DevOpsIngestion.Core.Ingestion.DevOpsIngestor;

namespace DevOpsIngestion.Api.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDevOpsIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DevOpsOptions>(configuration.GetSection(DevOpsOptions.SectionName));
        services.AddHttpClient<IDevOpsRepository, DevOpsRepository>(client =>
        {
            client.BaseAddress = new Uri(configuration.GetValue<string>($"{DevOpsOptions.SectionName}:OrganizationUrl") ?? "https://smartpoint-at.visualstudio.com");
        });

        return services;
    }
}

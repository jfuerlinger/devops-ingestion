using DevOpsIngestion.Core.Ingestion;
using DevOpsIngestion.Core.Ingestion.DevOpsIngestor;
using FastEndpoints;

namespace DevOpsIngestion.Api.Jobs.InitDevOpsIngestion
{
    internal sealed class InitDevOpsIngestionCommandHandler(
        ILogger<InitDevOpsIngestionCommandHandler> logger,
        IConfiguration configuration,
        DataIngestor dataIngestor,
        IDevOpsRepository devOpsRepository) 
        : ICommandHandler<InitDevOpsIngestionCommand>
    {
        public async Task ExecuteAsync(InitDevOpsIngestionCommand command, CancellationToken ct)
        {
            logger.LogInformation("DevOps ingestion process initialized.");

            var devOpsOrganization = configuration["DevOps:Project"] ?? throw new ArgumentException("DevOps:Project");
            
            await dataIngestor.IngestDataAsync(
                new DevOpsSource(devOpsRepository, devOpsOrganization));

            logger.LogInformation("DevOps ingestion process completed.");
        }
    }
}

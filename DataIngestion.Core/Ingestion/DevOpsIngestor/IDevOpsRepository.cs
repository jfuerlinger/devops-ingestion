using DevOpsIngestion.Core.Ingestion.DevOpsIngestor.Models;

namespace DevOpsIngestion.Core.Ingestion.DevOpsIngestor;

public interface IDevOpsRepository
{
    Task<IEnumerable<WorkItemInfo>> GetAllWorkItemInfosAsync(CancellationToken cancellationToken = default);
    Task<WorkItemDetails> LoadWorkItemByIdAsync(int workItemId, CancellationToken cancellationToken = default);
}
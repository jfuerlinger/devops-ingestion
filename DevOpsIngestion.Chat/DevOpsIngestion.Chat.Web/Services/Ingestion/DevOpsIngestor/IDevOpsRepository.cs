using DevOpsIngestion.Chat.Web.Services.Ingestion.DevOpsIngestor.Models;

namespace DevOpsIngestion.Chat.Web.Services.Ingestion.DevOpsIngestor;

public interface IDevOpsRepository
{
    Task<IEnumerable<WorkItemInfo>> GetAllWorkItemInfosAsync(CancellationToken cancellationToken = default);
    Task<WorkItemDetails> LoadWorkItemByIdAsync(int workItemId, CancellationToken cancellationToken = default);
}
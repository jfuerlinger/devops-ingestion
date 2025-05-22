namespace DevOpsIngestion.Core.Ingestion.DevOpsIngestor.Models;

public class WorkItemInfo
{
    public int WorkItemId { get; set; }
    public string WorkItemType { get; set; } = string.Empty;
    public DateTime LastModifiedDate { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
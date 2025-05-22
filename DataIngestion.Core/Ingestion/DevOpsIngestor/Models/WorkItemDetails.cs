namespace DevOpsIngestion.Core.Ingestion.DevOpsIngestor.Models;

public class WorkItemDetails : WorkItemInfo
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<WorkItemComment> Comments { get; set; } = new List<WorkItemComment>();
}

public class WorkItemComment
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
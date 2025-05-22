namespace DevOpsIngestion.Core.Ingestion.DevOpsIngestor;

public class DevOpsOptions
{
    public const string SectionName = "DevOps";
    
    public string OrganizationUrl { get; set; } = "https://smartpoint-at.visualstudio.com";
    public string Project { get; set; } = "smartpoint";
    public string PersonalAccessToken { get; set; } = string.Empty;
}
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevOpsIngestion.Chat.Web.Services.Ingestion.DevOpsIngestor.Models;
using Microsoft.Extensions.Options;

namespace DevOpsIngestion.Chat.Web.Services.Ingestion.DevOpsIngestor;

public class DevOpsRepository : IDevOpsRepository
{
    private readonly HttpClient _httpClient;
    private readonly DevOpsOptions _options;
    private readonly ILogger<DevOpsRepository> _logger;

    public DevOpsRepository(
        HttpClient httpClient,
        IOptions<DevOpsOptions> options,
        ILogger<DevOpsRepository> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // Configure the HttpClient for Azure DevOps API
        if (!string.IsNullOrEmpty(_options.PersonalAccessToken))
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
        
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<IEnumerable<WorkItemInfo>> GetAllWorkItemInfosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all work items of specified types (Feature, Bug, Requirement)
            var workItemTypes = new[] { "Feature", "Bug", "Requirement" };
            var workItemInfos = new List<WorkItemInfo>();
            
            foreach (var workItemType in workItemTypes)
            {
                var query = $@"
                    SELECT [System.Id]
                    FROM WorkItems 
                    WHERE [System.WorkItemType] = '{workItemType}'
                    ORDER BY [System.ChangedDate] DESC";
                
                // Use Azure DevOps WIQL API to query work items
                var wiqlRequest = new
                {
                    query
                };
                
                var wiqlJson = JsonSerializer.Serialize(wiqlRequest);
                var requestContent = new StringContent(wiqlJson, Encoding.UTF8, "application/json");
                
                var requestUrl = $"{_options.OrganizationUrl}/{_options.Project}/_apis/wit/wiql?api-version=7.0";
                var response = await _httpClient.PostAsync(requestUrl, requestContent, cancellationToken);
                
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var queryResult = JsonSerializer.Deserialize<WiqlQueryResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (queryResult?.WorkItems?.Any() == true)
                {
                    // Get work item details in batches to avoid exceeding API limits
                    const int batchSize = 100;
                    for (var i = 0; i < queryResult.WorkItems.Length; i += batchSize)
                    {
                        var batchIds = queryResult.WorkItems
                            .Skip(i)
                            .Take(batchSize)
                            .Select(wi => wi.Id)
                            .ToArray();
                        
                        var detailsUrl = $"{_options.OrganizationUrl}/{_options.Project}/_apis/wit/workitems?ids={string.Join(",", batchIds)}&fields=System.Id,System.WorkItemType,System.ChangedDate,System.AssignedTo,System.Url&api-version=7.0";
                        var detailsResponse = await _httpClient.GetAsync(detailsUrl, cancellationToken);
                        
                        detailsResponse.EnsureSuccessStatusCode();
                        
                        var detailsContent = await detailsResponse.Content.ReadAsStringAsync(cancellationToken);
                        var detailsResult = JsonSerializer.Deserialize<WorkItemDetailsResult>(detailsContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        if (detailsResult?.Value != null)
                        {
                            foreach (var item in detailsResult.Value)
                            {
                                var workItemInfo = new WorkItemInfo
                                {
                                    WorkItemId = item.Id,
                                    WorkItemType = item.Fields.WorkItemType ?? workItemType,
                                    LastModifiedDate = item.Fields.ChangedDate,
                                    Owner = item.Fields.AssignedTo?.DisplayName ?? string.Empty,
                                    Url = item.Url ?? string.Empty
                                };
                                
                                workItemInfos.Add(workItemInfo);
                            }
                        }
                    }
                }
            }
            
            return workItemInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work items from Azure DevOps");
            throw;
        }
    }
    
    public async Task<WorkItemDetails> LoadWorkItemByIdAsync(int workItemId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get detailed work item information including description
            var detailsUrl = $"{_options.OrganizationUrl}/{_options.Project}/_apis/wit/workitems/{workItemId}?fields=System.Id,System.WorkItemType,System.ChangedDate,System.AssignedTo,System.Title,System.Description,System.Url&api-version=7.0";
            var detailsResponse = await _httpClient.GetAsync(detailsUrl, cancellationToken);
            detailsResponse.EnsureSuccessStatusCode();
            
            var detailsContent = await detailsResponse.Content.ReadAsStringAsync(cancellationToken);
            var workItemDetail = JsonSerializer.Deserialize<WorkItemDetail>(detailsContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (workItemDetail == null)
            {
                throw new InvalidOperationException($"Work item {workItemId} not found");
            }
            
            var workItemDetails = new WorkItemDetails
            {
                WorkItemId = workItemDetail.Id,
                WorkItemType = workItemDetail.Fields.WorkItemType ?? string.Empty,
                LastModifiedDate = workItemDetail.Fields.ChangedDate,
                Owner = workItemDetail.Fields.AssignedTo?.DisplayName ?? string.Empty,
                Url = workItemDetail.Url ?? string.Empty,
                Title = workItemDetail.Fields.Title ?? string.Empty,
                Description = workItemDetail.Fields.Description ?? string.Empty
            };
            
            // Get the comments for the work item
            var commentsUrl = $"{_options.OrganizationUrl}/{_options.Project}/_apis/wit/workitems/{workItemId}/comments?api-version=7.0";
            var commentsResponse = await _httpClient.GetAsync(commentsUrl, cancellationToken);
            commentsResponse.EnsureSuccessStatusCode();
            
            var commentsContent = await commentsResponse.Content.ReadAsStringAsync(cancellationToken);
            var commentsResult = JsonSerializer.Deserialize<CommentsResult>(commentsContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (commentsResult?.Comments != null)
            {
                foreach (var comment in commentsResult.Comments)
                {
                    workItemDetails.Comments.Add(new WorkItemComment
                    {
                        Id = comment.Id,
                        Text = comment.Text,
                        CreatedBy = comment.CreatedBy?.DisplayName ?? string.Empty,
                        CreatedDate = comment.CreatedDate
                    });
                }
            }
            
            return workItemDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work item {WorkItemId} from Azure DevOps", workItemId);
            throw;
        }
    }
    
    // Helper classes for deserializing Azure DevOps API responses
    private class WiqlQueryResult
    {
        public WorkItemReference[] WorkItems { get; set; } = Array.Empty<WorkItemReference>();
    }
    
    private class WorkItemReference
    {
        public int Id { get; set; }
        public string? Url { get; set; }
    }
    
    private class WorkItemDetailsResult
    {
        public WorkItemDetail[] Value { get; set; } = Array.Empty<WorkItemDetail>();
    }
    
    private class WorkItemDetail
    {
        public int Id { get; set; }
        public string? Url { get; set; }
        public FieldValues Fields { get; set; } = new();
    }
    
    private class FieldValues
    {
        [JsonPropertyName("System.WorkItemType")]
        public string? WorkItemType { get; set; }
        
        [JsonPropertyName("System.ChangedDate")]
        public DateTime ChangedDate { get; set; }
        
        [JsonPropertyName("System.AssignedTo")]
        public AssignedToValue? AssignedTo { get; set; }
        
        [JsonPropertyName("System.Title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("System.Description")]
        public string? Description { get; set; }
    }
    
    private class AssignedToValue
    {
        public string DisplayName { get; set; } = string.Empty;
    }
    
    private class CommentsResult
    {
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }

    private class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public UserReference? CreatedBy { get; set; }
    }

    private class UserReference
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}
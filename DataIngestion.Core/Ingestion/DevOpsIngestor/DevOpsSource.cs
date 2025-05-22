using DevOpsIngestion.Core.Ingestion.DevOpsIngestor.Models;
using DevOpsIngestion.Core.Model;
using Microsoft.SemanticKernel.Text;

namespace DevOpsIngestion.Core.Ingestion.DevOpsIngestor;

public class DevOpsSource(
    IDevOpsRepository devOpsRepository,
    string organization)
    : IIngestionSource
{
    public string SourceId => $"{nameof(DevOpsSource)}:{organization}";

    public async Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var results = new List<IngestedDocument>();
        var workItems = await devOpsRepository.GetAllWorkItemInfosAsync();

        var existingDocumentsById = existingDocuments.ToDictionary(d => d.DocumentId);
        foreach (var workItem in workItems)
        {
            var sourceFileId = workItem.WorkItemId.ToString();
            var sourceFileVersion = workItem.LastModifiedDate.ToString();
            var existingDocumentVersion = existingDocumentsById.TryGetValue(sourceFileId, out var existingDocument) ? existingDocument.DocumentVersion : null;
            if (existingDocumentVersion != sourceFileVersion)
            {
                results.Add(new()
                {
                    Key = Guid.CreateVersion7(),
                    SourceId = SourceId,
                    DocumentId = sourceFileId,
                    DocumentVersion = sourceFileVersion
                });
            }
        }

        return results;
    }

    public async Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var currentWorkItems = await devOpsRepository.GetAllWorkItemInfosAsync();
        var currentFileIds = currentWorkItems.ToLookup(item => item.WorkItemId.ToString());
        var deletedDocuments = existingDocuments.Where(d => !currentFileIds.Contains(d.DocumentId));
        return deletedDocuments;
    }

    public async Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document)
    {
        var workItem = await devOpsRepository.LoadWorkItemByIdAsync(int.Parse(document.DocumentId));
        var comments = workItem.Comments.Select(comment => comment.Text);

        return GetWorkItemChunks(workItem)
            .Select(p => new IngestedChunk
            {
                Key = Guid.CreateVersion7(),
                DocumentId = document.DocumentId,
                PageNumber = p.PageNumber,
                Text = p.Text,
            });
    }

    private static IEnumerable<(int PageNumber, string Text)> GetWorkItemChunks(WorkItemDetails workItem)
    {
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only
        return TextChunker.SplitPlainTextParagraphs(
            [workItem.Description,
                    .. workItem.Comments
                            .Select(comment => comment.Text)], 200)
            .Select(text => (1, text));
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only
    }
}

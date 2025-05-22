using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace DevOpsIngestion.Chat.Web.Services.Ingestion.DevOpsIngestor;

public class DevOpsSource(IDevOpsRepository devOpsRepository) : IIngestionSource
{
    public static string SourceFileId(string path) => Path.GetFileName(path);
    public static string SourceFileVersion(string path) => File.GetLastWriteTimeUtc(path).ToString("o");

    public string SourceId => $"{nameof(DevOpsSource)}:{sourceDirectory}";

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



        return [workItem.Description, ... comments]
            .Select(p => new IngestedChunk
        {
            Key = Guid.CreateVersion7(),
            DocumentId = document.DocumentId,
            PageNumber = p.PageNumber,
            Text = p.Text,
        });
    }

    private static IEnumerable<(int PageNumber, int IndexOnPage, string Text)> GetPageParagraphs(Page pdfPage)
    {
        var letters = pdfPage.Letters;
        var words = NearestNeighbourWordExtractor.Instance.GetWords(letters);
        var textBlocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        var pageText = string.Join(Environment.NewLine + Environment.NewLine,
            textBlocks.Select(t => t.Text.ReplaceLineEndings(" ")));

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only
        return TextChunker.SplitPlainTextParagraphs([pageText], 200)
            .Select((text, index) => (pdfPage.Number, index, text));
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only
    }
}

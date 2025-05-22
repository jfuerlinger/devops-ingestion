using DevOpsIngestion.Core.Model;
using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace DevOpsIngestion.Core.Ingestion.PDFIngestor;

public class PDFSingleFileSource(
    string fileName, 
    DateTime modifiedOn, 
    Stream stream) 
    : IIngestionSource
{
    public string SourceId => $"{nameof(PDFSingleFileSource)}:{fileName}";

    public Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        return Task.FromResult<IEnumerable<IngestedDocument>>(
        [
            new() {
                Key = Guid.CreateVersion7(),
                SourceId = SourceId,
                DocumentId = fileName,
                DocumentVersion = modifiedOn.ToString()
            }
        ]);
    }

    public Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        return Task.FromResult((IEnumerable<IngestedDocument>)[]);
    }

    public Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document)
    {
        using var pdf = PdfDocument.Open(stream);
        var paragraphs = pdf.GetPages().SelectMany(GetPageParagraphs).ToList();

        return Task.FromResult(paragraphs.Select(p => new IngestedChunk
        {
            Key = Guid.CreateVersion7(),
            DocumentId = document.DocumentId,
            PageNumber = p.PageNumber,
            Text = p.Text,
        }));
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

using DevOpsIngestion.Core.Ingestion;
using DevOpsIngestion.Core.Ingestion.PDFIngestor;
using FastEndpoints;
using static DevOpsIngestion.Api.Features.Ingestion.Pdf.IngestPdfDocuments;

namespace DevOpsIngestion.Api.Features.Ingestion.Pdf
{
    internal sealed class IngestPdfDocuments(
        ILogger<IngestPdfDocuments> logger,
        DataIngestor dataIngestor)
        : Endpoint<IngestPdfDocumentsRequest>
    {
        public override void Configure()
        {
            Post("/ingestion/pdfs");
            AllowFileUploads();
            AllowAnonymous();
        }

        public override async Task HandleAsync(IngestPdfDocumentsRequest req, CancellationToken ct)
        {
            logger.LogInformation("Ingesting PDF {FileCount} documents ...", Files.Count);
            foreach (var file in Files)
            {
                if (file.Length > 0)
                {
                    logger.LogInformation("Ingesting PDF {fileName} ...", file.FileName);
                    await dataIngestor.IngestDataAsync(new PDFSingleFileSource(
                        file.FileName, DateTime.Now,
                        file.OpenReadStream()));
                }
            }
            
            await SendNoContentAsync();
        }

        internal sealed class IngestPdfDocumentsRequest
        {
            public IFormFile? File1 { get; set; }
            public IFormFile? File2 { get; set; }
            public IFormFile? File3 { get; set; }
            public IFormFile? File4 { get; set; }
            public IFormFile? File5 { get; set; }
        }
    }
}

using FastEndpoints;

namespace DevOpsIngestion.Api.Features.Ingestion.Pdf
{
    internal class ResetAllPdfIngestions(
        ILogger<ResetAllPdfIngestions> logger) : EndpointWithoutRequest
    {
        public override void Configure()
        {
            Delete("/ingestion/pdfs");
            AllowAnonymous();
            Summary(s => s.Summary = "Reset all PDF ingestion results.");
        }

        override public async Task HandleAsync(CancellationToken ct)
        {
            logger.LogInformation("Resetting all PDF ingestion results ...");
            //await DataIngestor.ResetIngestionAsync(HttpContext.RequestServices, ct);
            await SendOkAsync(ct);
        }
    }
}

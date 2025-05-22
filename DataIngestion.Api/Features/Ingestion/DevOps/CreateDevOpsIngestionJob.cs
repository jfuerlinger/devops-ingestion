using DevOpsIngestion.Api.Jobs.InitDevOpsIngestion;
using FastEndpoints;
using FluentValidation;
using static DevOpsIngestion.Api.Features.Ingestion.DevOps.CreateDevOpsIngestionJob;

namespace DevOpsIngestion.Api.Features.Ingestion.DevOps
{
    internal class CreateDevOpsIngestionJob(
        ILogger<CreateDevOpsIngestionJob> logger)
        : EndpointWithoutRequest<CreateDevOpsIngestionJobResponse>
    {
        public override void Configure()
        {
            Post("/ingestion/devops");
            AllowAnonymous();
            Summary(s => s.Summary = "Initialize DevOps ingestion process.");
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            logger.LogInformation("Enquing InitDevOpsIngestionCommand ...");
            var trackingId = await new InitDevOpsIngestionCommand { }.QueueJobAsync(ct: ct);
            logger.LogInformation("InitDevOpsIngestionCommand enqued.");

            await SendCreatedAtAsync<GetDevOpsIngestionJobState>(
                routeValues: new { id = trackingId },
                new CreateDevOpsIngestionJobResponse(trackingId),
                cancellation: ct);
        }

        internal sealed class CreateDevOpsIngestionJobResponse
        {
            public Guid TrackingId { get; set; }

            public CreateDevOpsIngestionJobResponse(Guid trackingId) => TrackingId = trackingId;

            class Validator : Validator<CreateDevOpsIngestionJobResponse>
            {
                public Validator()
                {
                    RuleFor(x => x.TrackingId)
                        .NotNull()
                        .WithMessage("Route-parameter 'id' is required");
                }
            }
        }
    }
}

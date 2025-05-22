using FastEndpoints;
using FluentValidation;
using static DevOpsIngestion.Api.Features.Ingestion.DevOps.GetDevOpsIngestionJobState;

namespace DevOpsIngestion.Api.Features.Ingestion.DevOps
{
    internal class GetDevOpsIngestionJobState(
        ILogger<GetDevOpsIngestionJobState> logger) 
        : Endpoint<GetDevOpsIngestionJobStateRequest, GetDevOpsIngestionJobStateResponse>
    {
        public override void Configure()
        {
            Get("/ingestion/devops/{id}/state");
            Summary(s =>
            {
                s.Summary = "Reports the state of the devops ingestion job with the specific tracking id";
                s.ExampleRequest = new GetDevOpsIngestionJobStateRequest { TrackingId = new Guid("556bde69-6b97-ef11-b85f-005056adb319") };
            });
            AllowAnonymous();
        }

        public override async Task HandleAsync(GetDevOpsIngestionJobStateRequest req, CancellationToken ct)
        {
            logger.LogInformation("Getting state of devops ingestion job {TrackingId} ...", req.TrackingId);

            await SendOkAsync(new GetDevOpsIngestionJobStateResponse(), ct);
        }


        internal class GetDevOpsIngestionJobStateRequest
        {
            [BindFrom("id")]
            public Guid TrackingId { get; set; }

            class Validator : Validator<GetDevOpsIngestionJobStateRequest>
            {
                public Validator()
                {
                    RuleFor(x => x.TrackingId)
                        .NotEqual(Guid.Empty)
                        .NotNull()
                        .WithMessage("Route-parameter 'id' is required");
                }
            }
        };

        internal class GetDevOpsIngestionJobStateResponse()
        {
            public string State { get; set; } = "Ingesting";
            public string Message { get; set; } = "Ingestion is in progress";
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
            public double Progress { get; set; } = 0;
            public int Total { get; set; } = 100;
            public int Processed { get; set; } = 0;

            class Validator : Validator<GetDevOpsIngestionJobStateResponse>
            {
                public Validator()
                {
                    RuleFor(x => x.State)
                        .NotEmpty()
                        .NotNull();

                    RuleFor(x => x.Message)
                        .NotNull()
                        .NotEmpty();

                    RuleFor(x => x.LastUpdated)
                        .LessThanOrEqualTo(DateTime.Now);

                    RuleFor(x => x.Progress)
                        .LessThanOrEqualTo(100)
                        .GreaterThanOrEqualTo(0)
                        .WithMessage("Progress must be between 0.0 and 100.0");
                }
            }
        };
    }
}

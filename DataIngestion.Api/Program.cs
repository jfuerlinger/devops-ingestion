using DevOpsIngestion.Api.Extensions;
using DevOpsIngestion.Api.Storage;
using DevOpsIngestion.Core.Ingestion;
using DevOpsIngestion.Core.Model;
using DevOpsIngestion.Core.Services;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services
    .AddFastEndpoints()
    .AddJobQueues<JobRecord, JobStorageProvider>()
    .SwaggerDocument();

builder.AddSqlServerDbContext<JobDbContext>(connectionName: "ingestion-db");

builder.Services.AddDevOpsIntegration(builder.Configuration);

var openai = builder.AddAzureOpenAIClient("openai");
openai.AddChatClient("gpt-4o-mini")
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());
openai.AddEmbeddingGenerator("text-embedding-3-small");

builder.AddQdrantClient("vectordb");
builder.Services.AddQdrantCollection<Guid, IngestedChunk>("data-devopsingestion_chat-chunks");
builder.Services.AddQdrantCollection<Guid, IngestedDocument>("data-devopsingestion_chat-documents");
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();
app.UseFastEndpoints()
   .UseSwaggerGen()
   .UseJobQueues(o => o.MaxConcurrency = 1);

app.Run();

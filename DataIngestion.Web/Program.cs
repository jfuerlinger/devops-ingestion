using Microsoft.Extensions.AI;
using DataIngestion.Web.Components;
using DataIngestion.Web.Services;
using DevOpsIngestion.Core.Model;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        var openai = builder.AddAzureOpenAIClient("openai");
        openai.AddChatClient("gpt-4o-mini")
            .UseFunctionInvocation()
            .UseOpenTelemetry(configure: c =>
                c.EnableSensitiveData = builder.Environment.IsDevelopment());
        openai.AddEmbeddingGenerator("text-embedding-3-small");

        builder.AddQdrantClient("vectordb");
        builder.Services.AddQdrantCollection<Guid, IngestedChunk>("data-devopsingestion_chat-chunks");
        builder.Services.AddQdrantCollection<Guid, IngestedDocument>("data-devopsingestion_chat-documents");
        //builder.Services.AddScoped<DataIngestor>();
        builder.Services.AddSingleton<SemanticSearch>();


        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseAntiforgery();

        app.UseStaticFiles();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
        // other sources by implementing IIngestionSource.
        // Important: ensure that any content you ingest is trusted, as it may be reflected back
        // to users or could be a source of prompt injection risk.
        //await DataIngestor.IngestDataAsync(
        //    app.Services,
        //    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

        //string devOpsOrganization = app.Configuration["DevOps:Project"] ?? throw new ArgumentException("DevOps:Project");
        //await DataIngestor.IngestDataAsync(
        //    app.Services,
        //    new DevOpsSource(
        //        app.Services.GetRequiredService<IDevOpsRepository>(),
        //        devOpsOrganization));

        app.Run();
    }
}
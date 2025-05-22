using k8s.Models;

var builder = DistributedApplication.CreateBuilder(args);

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://YOUR-DEPLOYMENT-NAME.openai.azure.com;Key=YOUR-API-KEY"
var openai = builder.AddConnectionString("openai");

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = sqlServer.AddDatabase("ingestion-db");

var api = builder.AddProject<Projects.DataIngestion_Api>("api")
    .WithReference(openai)
    .WithReference(vectorDB)
    .WithReference(db)
    .WaitFor(db)
    .WaitFor(vectorDB);

api.WithUrl($"{api.GetEndpoint("https")}/swagger", "Swagger");

var webApp = builder.AddProject<Projects.DataIngestion_Web>("webapp");
webApp.WithReference(openai);
webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB)
    .WaitFor(api);

builder.Build().Run();

using ACSBounceSuppression.Services;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder()
   .ConfigureFunctionsWebApplication() // Correct method for ASP.NET Core Integration  
   .ConfigureServices(services =>
   {
       // Service to manage the suppression list  
       services.AddSingleton<EmailSuppressionService>();

       // BlobServiceClient for writing to Storage Account  
       services.AddSingleton(x =>
       {
           var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnection");
           return new BlobServiceClient(storageConnectionString);
       });

       // Application Insights (optional but recommended)  
       services.AddApplicationInsightsTelemetryWorkerService();
       services.ConfigureFunctionsApplicationInsights();
   })
   .Build();

host.Run();

using FlowDance.AzureFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        services.AddTransient<IDetermineCompensation, DetermineCompensationService>();
        services.AddTransient<IStorage, Storage>();

        services.AddHttpClient();
    })
    .Build();

host.Run();

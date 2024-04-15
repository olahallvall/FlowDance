using FlowDance.AzureFunctions.Services;
using FlowDance.Common.RabbitMQUtils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddLogging();

        services.AddTransient<IDetermineCompensation>((s) => {
            return new DetermineCompensationService(null);
        });
        services.AddTransient<IStorage>((s) => {
            return new Storage(null);
        });
    })
    .Build();

host.Run();

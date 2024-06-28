using FlowDance.AzureFunctions.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        if (hostContext.HostingEnvironment.IsDevelopment())
        {
            config.AddUserSecrets<Program>();
        }

        config.AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        //services.Configure<KestrelServerOptions>(options =>
        //{
        //    options.AllowSynchronousIO = true;
        //});

        services.AddTransient<IDetermineCompensation, DetermineCompensationService>();
        services.AddTransient<IStorage, Storage>();

        services.AddHttpClient();
    })
    .Build();

host.Run();

using FlowDance.Server.Services;
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
    .ConfigureServices((hostBuilderContext,  services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = hostBuilderContext.Configuration["Redis_Connection"];
            options.InstanceName = "FlowDance";
        });

        services.AddTransient<ISpanCommandService, SpanCommandService>();
        services.AddTransient<ISpanEventService, SpanEventService>();

        services.AddTransient<IStorageStreamService, StorageStreamService>();
        services.AddTransient<IStorageQueueService, StorageQueueService>();

        services.AddTransient<ISpanEventUtilService, SpanEventUtilService>();
        
        services.AddHttpClient();
    })
    .Build();

host.Run();

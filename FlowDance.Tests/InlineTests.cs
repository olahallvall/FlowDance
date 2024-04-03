using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using FlowDance.Client;

namespace FlowDance.Tests;

[TestClass]
public class InlineTests
{
    private static ILoggerFactory _factory = null!;
    private static IConfigurationRoot _config = null!;

    [ClassInitialize()]
    public static void ClassInit(TestContext context)
    {
        _factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddFilter("RabbitMQ.Stream", LogLevel.Information);
        });

        _config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
    }

    [TestMethod]
    public void OneLevelCompensationScope()
    {
        var guid = Guid.NewGuid();

        using (CompensationScope compScope = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {

            // Boka taxi


            // Boka flyg


            compScope.Commit();
        }

    }
}
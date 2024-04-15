using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FlowDance.Common.RabbitMQUtils;

namespace FlowDance.Tests;

[TestClass]
public class StorageTests
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
    public void ReadAllSpansFromStream()
    {
        var storage = new Storage(_factory);
        var spanList = storage.ReadAllSpansFromStream("119e60ec-f046-45f5-b880-fadb7e9da3c4");

        spanList.Count();
    }

    [TestMethod]
    public void GetStreamSystem()
    {
        var storage = new Storage(_factory);
        storage.CreateStream(new Guid().ToString());

    }
}
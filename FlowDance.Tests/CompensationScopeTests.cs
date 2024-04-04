using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using FlowDance.Client;
using FlowDance.Client.RabbitMQUtils;

namespace FlowDance.Tests;

[TestClass]
public class CompensationScopeTests
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
    public void ParentCompensationScope()
    {
        var guid = Guid.NewGuid();

        var storage = new Storage(_factory);

        using (CompensationScope compScope = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            compScope.Commit();
        }

        Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString()).Count(),2);
    }

    [TestMethod]
    public void ParentChildCompensationScope()
    {
        var guid = Guid.NewGuid();

        // Parent
        using (CompensationScope compScopeParent = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            // Child
            using (CompensationScope compScopeChild = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
            {
                compScopeChild.Commit();
            }

            compScopeParent.Commit();
        }

        var storage = new Storage(_factory);
        Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString()).Count(), 4);
    }

    [TestMethod]
    public void ParentParentCompensationScope()
    {
        var guid = Guid.NewGuid();

        // Parent1
        using (CompensationScope compScopeParent1 = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            compScopeParent1.Commit();
        }

        // Parent2
        using (CompensationScope compScopeParent2 = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            compScopeParent2.Commit();
        }

        //var storage = new Storage(_factory);
        //Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString()).Count(), 4);
    }
}

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
    public void RootCompensationScope()
    {
        var guid = Guid.NewGuid();

        var storage = new Storage(_factory);

        using (CompensationScope compScope = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            compScope.Complete();
        }

        Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString()).Count(), 2);

        storage.DeleteStream(guid.ToString());
    }

    [TestMethod]
    public void RootWithInnerCompensationScope()
    {
        var guid = Guid.NewGuid();

        // The top-most compensation scope is referred to as the root scope.
        // Root scope
        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            // Inner scope
            using (CompensationScope compScopeInner = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
            {
                compScopeInner.Complete();
            }

            compScopeRoot.Complete();
        }

        var storage = new Storage(_factory);
        Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString()).Count(), 4);
        
        storage.DeleteStream(guid.ToString());
    }

    [TestMethod]
    public void RootMethodWithTwoInnerMethodCompensationScope()
    {
        var guid = Guid.NewGuid();
        RootMethod(guid);

        var storage = new Storage(_factory);
        Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString()).Count(), 4);

        storage.DeleteStream(guid.ToString());
    }

    private void RootMethod(Guid guid)
    {
        using (CompensationScope compScope = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            /* Perform transactional work here */
            InnerMethod(guid);
            compScope.Complete();
        }
    }

    private void InnerMethod(Guid guid)
    {
        using (CompensationScope compScope = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            /* Perform transactional work here */
            compScope.Complete();
        }
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public void MultipleRootCompensationScopeUsingSameTraceId()
    {
        var guid = Guid.NewGuid();

        // Root
        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            compScopeRoot.Complete();
        }

        // Root
        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
            compScopeRoot.Complete();
        }
    }

    [TestMethod]
    public void RootMethodWithTwoInlineCompensationScope()
    {
        var guid = Guid.NewGuid();

        // Root
        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", guid, _factory))
        {
              // Inner scope 1
            using (CompensationScope compScopeInner = new CompensationScope("http://localhost/HotelService/Compensation1", guid, _factory))
            {
                compScopeInner.Complete();
            }

              // Inner scope 2
            using (CompensationScope compScopeInner = new CompensationScope("http://localhost/HotelService/Compensation2", guid, _factory))
            {
                compScopeInner.Complete();
            }
            
            compScopeRoot.Complete();
        }
        
        var storage = new Storage(_factory);
        Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString()).Count(), 6);

        storage.DeleteStream(guid.ToString());
    }
}

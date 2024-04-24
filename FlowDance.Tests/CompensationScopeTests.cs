using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using FlowDance.Client;
using FlowDance.Client.RabbitMq;

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
        var traceId = Guid.NewGuid();

        var storage = new Storage(_factory);

        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/TripBookingService/Compensation", traceId, _factory))
        {
            compScopeRoot.Complete();
        }
    }

    [TestMethod]
    public void RootWithInnerCompensationScope()
    {
        var traceId = Guid.NewGuid();

        // The top-most compensation scope is referred to as the root scope.
        // Root scope
        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/TripBookingService/Compensation", traceId, _factory))
        {
            // Inner scope
            using (CompensationScope compScopeInnerCar = new CompensationScope("http://localhost/CarService/Compensation", traceId, _factory))
            {
                compScopeInnerCar.Complete();
            }

            compScopeRoot.Complete();
        }
    }

    [TestMethod]
    public void RootMethodWithTwoInnerMethodCompensationScope()
    {
        var traceId = Guid.NewGuid();
        RootMethod(traceId);
    }

    private void RootMethod(Guid traceId)
    {
        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/TripBookingService/Compensation", traceId, _factory))
        {
            /* Perform transactional work here */
            InnerMethod(traceId);

            compScopeRoot.Complete();
        }
    }

    private void InnerMethod(Guid traceId)
    {
        using (CompensationScope compScopeInner = new CompensationScope("http://localhost/CarService/Compensation", traceId, _factory))
        {
            /* Perform transactional work here */

            compScopeInner.Complete();
        }
    }

    [TestMethod]
    public void MultipleRootCompensationScopeUsingSameTraceId()
    {
        var newGuid = Guid.NewGuid();

        // Root
        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", newGuid, _factory))
        {
            /* Perform transactional work here */

            compScopeRoot.Complete();
        }

        // Root
        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", newGuid, _factory))
        {
            /* Perform transactional work here */

            compScopeRoot.Complete();
        }
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public void CompensationScopeThrowingException()
    {
        var traceId = Guid.NewGuid();

        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", traceId, _factory))
        {
            /* Perform transactional work here */
            throw new Exception("Something bad has happened!");

            // Will not run
            compScopeRoot.Complete();
        }
    }

    [TestMethod]
    public void RootMethodWithTwoInlineCompensationScope()
    {
        var traceId = Guid.NewGuid();

        // Root
        using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/TripBookingService/Compensation", traceId, _factory))
        {
            /* Perform transactional work here */

            // Inner scope 1
            using (CompensationScope compScopeInner = new CompensationScope("http://localhost/CarService/Compensation", traceId, _factory))
            {
                /* Perform transactional work here */

                compScopeInner.Complete();
            }

              // Inner scope 2
            using (CompensationScope compScopeInner = new CompensationScope("http://localhost/HotelService/Compensation2", traceId, _factory))
            {
                /* Perform transactional work here */

                compScopeInner.Complete();
            }
            
            compScopeRoot.Complete();
        }
    }
}

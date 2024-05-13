using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using FlowDance.Client;
using FlowDance.Tests.RabbitMqHttpApiClient.API;
using FlowDance.Common.Models;

namespace FlowDance.Tests;

[TestClass]
public class CompensationSpanTests
{
    private static ILoggerFactory _factory = null!;
    private static IConfigurationRoot _config = null!;
    private RabbitMqApi _rabbitMqApi = new RabbitMqApi("http://localhost:15672", "guest", "guest");

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
    public void RootCompensationSpan()
    {
        var traceId = Guid.NewGuid();

        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _factory))
        {
            /* Perform transactional work here */
            compSpanRoot.Complete();
        }

        Thread.Sleep(10000);
        Assert.AreEqual(2, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public void RootWithInnerCompensationSpan()
    {
        var traceId = Guid.NewGuid();

        // The top-most compensation scope is referred to as the root scope.
        // Root scope
        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation", "postdata1223", new Dictionary<string, string>() { { "KeyB", "656565" } }), traceId, _factory))
        {
            // Inner scope
            using (var compSpanInnerCar = new CompensationSpan(new HttpCompensatingAction("http://localhost/CarService/Compensation", "postdata76767"), traceId, _factory))
            {
                /* Perform transactional work here */
                throw new Exception("Something bad has happened!");

                compSpanInnerCar.Complete();
            }

            compSpanRoot.Complete();
        }
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public void RootMethodWithTwoInnerMethodCompensationSpan()
    {
        var traceId = Guid.NewGuid();
        RootMethod(traceId);
    }

    private void RootMethod(Guid traceId)
    {
        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _factory))
        {
            /* Perform transactional work here */
            InnerMethod(traceId);

            compSpanRoot.Complete();
        }
    }

    private void InnerMethod(Guid traceId)
    {
        using (var compSpanInner = new CompensationSpan(new HttpCompensatingAction("http://localhost/CarService/Compensation"), traceId, _factory))
        {
            /* Perform transactional work here */
            throw new Exception("Something bad has happened!");

            compSpanInner.Complete();
        }
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public void MultipleRootCompensationSpanUsingSameTraceId()
    {
        var newGuid = Guid.NewGuid();

        // Root
        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/HotelService/Compensation"), newGuid, _factory))
        {
            /* Perform transactional work here */
            throw new Exception("Something bad has happened!");

            compSpanRoot.Complete();
        }

        // Root
        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/CarService/Compensation"), newGuid, _factory))
        {
            /* Perform transactional work here */

            compSpanRoot.Complete();
        }
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public void RootMethodWithTwoInlineCompensationSpan()
    {
        var traceId = Guid.NewGuid();

        // Root
        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _factory))
        {
            /* Perform transactional work here */

            // Inner scope 1
            using (var compSpanInner = new CompensationSpan(new HttpCompensatingAction("http://localhost/CarService/Compensation"), traceId, _factory))
            {
                /* Perform transactional work here */

                compSpanInner.Complete();
            }

              // Inner scope 2
            using (var compSpanInner = new CompensationSpan(new HttpCompensatingAction("http://localhost/HotelService/Compensation"), traceId, _factory))
            {
                /* Perform transactional work here */
                throw new Exception("Something bad has happened!");

                compSpanInner.Complete();
            }
            
            compSpanRoot.Complete();
        }
    }
}

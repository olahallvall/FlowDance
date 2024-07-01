using FlowDance.Client;
using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Enums;
using FlowDance.Tests.RabbitMqHttpApiClient.API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _factory, CompensationSpanOption.RequiresNewBlockingCallChain))
        {
            compSpanRoot.AddCompensationData("SomeDataYouWantToByAbleToRollbackTo", "QC");

            /* Perform transactional work here */
            compSpanRoot.Complete("SomeDataYouWantToByAbleToRollbackTo1", "QC1");
        }

        Thread.Sleep(10000);
        Assert.AreEqual(4, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public void RootCompensationSpanMissingCompensationSpanOption()
    {
        var traceId = Guid.NewGuid();

        try
        {
            using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _factory))
            {
                /* Perform transactional work here */
                compSpanRoot.Complete();
            }
        }
        finally
        {
            Thread.Sleep(10000);
            Assert.AreEqual(1, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
        }
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public void RootWithInnerCompensationSpan()
    {
        var traceId = Guid.NewGuid();

        // The top-most compensation scope is referred to as the root scope.
        // Root scope
        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation", new Dictionary<string, string>() { { "KeyB", "656565" } }), traceId, _factory, CompensationSpanOption.RequiresNewBlockingCallChain))
        {
            compSpanRoot.AddCompensationData("SomeDataYouWantToByAbleToRollbackToForTheTrip", "TripBegin");

            // Inner scope
            using (var compSpanInnerCar = new CompensationSpan(new HttpCompensatingAction("http://localhost/CarService/Compensation"), traceId, _factory))
            {
                /* Perform transactional work here */

                compSpanInnerCar.AddCompensationData("SomeDataYouWantToByAbleToRollbackToForTheCar1", "Car1");

                compSpanInnerCar.AddCompensationData("SomeDataYouWantToByAbleToRollbackToForTheCar11", "Car11");

                throw new Exception("Something bad has happened!");

                compSpanInnerCar.Complete("SomeDataYouWantToByAbleToRollbackToForTheCar2", "Car2");
            }

            compSpanRoot.Complete("SomeDataYouWantToByAbleToRollbackToForTheTrip", "TripEnd");
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
        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _factory, CompensationSpanOption.RequiresNewBlockingCallChain))
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
    public void MultipleCompensationSpanUsingSameTraceId()
    {
        var newGuid = Guid.NewGuid();

        // Root
        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/HotelService/Compensation"), newGuid, _factory, CompensationSpanOption.RequiresNewNonBlockingCallChain))
        {
            /* Perform transactional work here */
            throw new Exception("Something bad has happened!");

            compSpanRoot.Complete();
        }

        
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
        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _factory, CompensationSpanOption.RequiresNewBlockingCallChain))
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

    [TestMethod]
    public void LooooooongRunningSpan()
    {
        var traceId = Guid.NewGuid();

        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _factory, CompensationSpanOption.RequiresNewBlockingCallChain))
        {
            /* Perform transactional work here */
            compSpanRoot.Complete();
        }

        Thread.Sleep(360000); // 6 minutes
        Assert.AreEqual(2, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
    }
}

using FlowDance.Client;
using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Enums;
using FlowDance.Common.Exceptions;
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
    public void RequiresNewBlockingCallChainCompensationSpan()
    {
        var traceId = Guid.Empty;

        using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _factory, CompensationSpanOption.RequiresNewBlockingCallChain))
        {
            traceId = compSpanRoot.TraceId;

            compSpanRoot.AddCompensationData("SomeDataYouWantToByAbleToRollbackTo", "QC");

            /* Perform transactional work here */
            compSpanRoot.Complete("SomeDataYouWantToByAbleToRollbackTo1", "QC1");
        }

        Thread.Sleep(10000);
        Assert.AreEqual(4, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
    }

    [TestMethod]
    public void RequiresNewBlockingCallChainMissingCompensationSpanOption()
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
        catch (CompensationSpanCreationException) 
        {
        }
        finally
        {
            Thread.Sleep(10000);
            Assert.AreEqual(1, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
        }
    }

    [TestMethod]
    public void RequiresNewNonBlockingCallChainCompensationSpansUsingSameTraceId()
    {
        var traceId = Guid.Empty;

        try
        {
            // Root one
            using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/HotelService/Compensation"), traceId, _factory, CompensationSpanOption.RequiresNewNonBlockingCallChain))
            {
                traceId = compSpanRoot.TraceId;
                compSpanRoot.Complete();
            }

            // Root two
            using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/CarService/Compensation"), traceId, _factory, CompensationSpanOption.RequiresNewNonBlockingCallChain))
            {
                compSpanRoot.Complete();
            }
        }
        catch (CompensationSpanValidationException)
        {
        }
        finally
        {
            Thread.Sleep(10000);
            Assert.AreEqual(2, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
        }
    }

    [TestMethod]
    public void CompensationSpanStoreEventInQueue()
    {
        var traceId = Guid.Empty;

        try
        {
            using (var compSpanRoot = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService1212/Compensation"), traceId, _factory, CompensationSpanOption.RequiresNewBlockingCallChain))
            {
                traceId = compSpanRoot.TraceId;

                using (var compSpan = new CompensationSpan(new HttpCompensatingAction("http://localhost/CarBookingService1212/Compensation"), traceId, _factory, CompensationSpanOption.Required))
                {
                    // No setting Complete makes this Span to be set as SpanClosedBattered
                    // A SpanClosedBattered will be added to the FlowDance.SpanEvents queue.
                    // compSpanRoot.Complete();
                }

                compSpanRoot.Complete();
            }
        }
        finally
        {
            Thread.Sleep(10000);
            Assert.AreEqual(4, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
        }
    }
}

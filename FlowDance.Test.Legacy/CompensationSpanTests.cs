using System;
using System.Threading;
using FlowDance.Client.Legacy;
using FlowDance.Client.Legacy.RabbitMq;
using FlowDance.Test.Legacy.RabbitMqHttpApiClient.API;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowDance.Test.Legacy
{
    [TestClass]
    public class CompensationSpanTests
    {
        private static ILoggerFactory _loggerFactory;
        private RabbitMqApi _rabbitMqApi = new RabbitMqApi("http://localhost:15672", "guest", "guest");

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            }); 
        }

        [TestMethod]
        public void RootCompensationSpan()
        {
            var traceId = Guid.NewGuid();

            using (var compSpan = new CompensationSpan("http://localhost/TripBookingService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */
                // DoSomething()

                compSpan.Complete();
            }

            Thread.Sleep(10000);
            Assert.AreEqual(2, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
        }

        [TestMethod]
        public void RootWithInnerCompensationSpan()
        {
            var traceId = Guid.NewGuid();

            // The top-most compensation scope is referred to as the root scope.
            // Root span
            using (var compSpanRoot = new CompensationSpan("http://localhost/TripBookingService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */

                // Inner scope
                using (var compSpanInner = new CompensationSpan("http://localhost/CarService/Compensation", traceId, _loggerFactory))
                {
                    /* Perform transactional work here */

                    compSpanInner.Complete();
                }
                 
                compSpanRoot.Complete();
            }

            Thread.Sleep(10000);
            Assert.AreEqual(4, _rabbitMqApi.GetQueueByVhostAndName("/", traceId.ToString()).Result.MessagesReady);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void RootMethodWithTwoInnerMethodCompensationSpan()
        {
            var guid = Guid.NewGuid();
            RootMethod(guid);
        }

        private void RootMethod(Guid guid)
        {
            using (var compSpan = new CompensationSpan("http://localhost/HotelService/Compensation", guid, _loggerFactory))
            {
                /* Perform transactional work here */
                InnerMethod(guid);
                compSpan.Complete();
            }
        }

        private void InnerMethod(Guid guid)
        {
            using (var compSpan = new CompensationSpan("http://localhost/HotelService/Compensation", guid, _loggerFactory))
            {
                /* Perform transactional work here */
                throw new Exception("Something bad has happened!");

                compSpan.Complete();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void MultipleRootCompensationSpanUsingSameTraceId()
        {
            var traceId = Guid.NewGuid();

            // Root
            using (var compSpanRoot = new CompensationSpan("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */
                throw new Exception("Something bad has happened!");

                compSpanRoot.Complete();
            }

            // Root
            using (var compSpanRoot = new CompensationSpan("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
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
            using (var compSpanRoot = new CompensationSpan("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */

                // Inner scope 1
                using (var compSpanInner = new CompensationSpan("http://localhost/HotelService/Compensation1", traceId, _loggerFactory))
                {
                    /* Perform transactional work here */

                    compSpanInner.Complete();
                }

                // Inner scope 2
                using (var compSpanInner = new CompensationSpan("http://localhost/HotelService/Compensation2", traceId, _loggerFactory))
                {
                    /* Perform transactional work here */
                    throw new Exception("Something bad has happened!");

                    compSpanInner.Complete();
                }

                compSpanRoot.Complete();
            }
        }
    }
}

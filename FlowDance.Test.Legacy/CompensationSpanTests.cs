using System;
using FlowDance.Client.Legacy;
using FlowDance.Client.Legacy.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowDance.Test.Legacy
{
    [TestClass]
    public class CompensationSpanTests
    {
        private static ILoggerFactory _loggerFactory;
     
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

            using (CompensationSpan compSpan = new CompensationSpan("http://localhost/TripBookingService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */
                // DoSomething()

                compSpan.Complete();
            }
        }

        [TestMethod]
        public void RootWithInnerCompensationSpan()
        {

            var traceId = Guid.NewGuid();

            // The top-most compensation span is referred to as the root span.
            // Root span
            using (CompensationSpan compSpanRoot = new CompensationSpan("http://localhost/TripBookingService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */

                // Inner span
                using (CompensationSpan compSpanInner = new CompensationSpan("http://localhost/CarService/Compensation", traceId, _loggerFactory))
                {
                    /* Perform transactional work here */

                    compSpanInner.Complete();
                }
                 
                compSpanRoot.Complete();
            }
        }

        [TestMethod]
        public void RootMethodWithTwoInnerMethodCompensationSpan()
        {
            var guid = Guid.NewGuid();
            RootMethod(guid);

            //Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString(), null).Count(), 4);

            //storage.DeleteStream(guid.ToString());
        }

        private void RootMethod(Guid guid)
        {
            using (CompensationSpan compSpan = new CompensationSpan("http://localhost/HotelService/Compensation", guid, _loggerFactory))
            {
                /* Perform transactional work here */
                InnerMethod(guid);
                compSpan.Complete();
            }
        }

        private void InnerMethod(Guid guid)
        {
            using (CompensationSpan compSpan = new CompensationSpan("http://localhost/HotelService/Compensation", guid, _loggerFactory))
            {
                /* Perform transactional work here */

                compSpan.Complete();
            }
        }

        [TestMethod]
        public void MultipleRootCompensationSpanUsingSameTraceId()
        {
            var traceId = Guid.NewGuid();

            // Root
            using (CompensationSpan compSpanRoot = new CompensationSpan("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */

                compSpanRoot.Complete();
            }

            // Root
            using (CompensationSpan compSpanRoot = new CompensationSpan("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */

                compSpanRoot.Complete();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CompensationSpanThrowingException()
        {
            var traceId = Guid.NewGuid();

            using (CompensationSpan compSpanRoot = new CompensationSpan("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */
                throw new Exception("Something bad has happened!");

                // Will not run
                compSpanRoot.Complete();
            }
        }

        [TestMethod]
        public void RootMethodWithTwoInlineCompensationSpan()
        {
            var traceId = Guid.NewGuid();

            // Root
            using (CompensationSpan compSpanRoot = new CompensationSpan("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */

                // Inner span 1
                using (CompensationSpan compSpanInner = new CompensationSpan("http://localhost/HotelService/Compensation1", traceId, _loggerFactory))
                {
                    /* Perform transactional work here */

                    compSpanInner.Complete();
                }

                // Inner span 2
                using (CompensationSpan compSpanInner = new CompensationSpan("http://localhost/HotelService/Compensation2", traceId, _loggerFactory))
                {
                    /* Perform transactional work here */

                    compSpanInner.Complete();
                }

                compSpanRoot.Complete();
            }

            var storage = new Storage(_loggerFactory);
            //Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString(), null).Count(), 6);

            //storage.DeleteStream(guid.ToString());
        }
    }


}

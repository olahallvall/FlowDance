using System;
using FlowDance.Client;
using FlowDance.Common.Models;
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

            using (var compSpan = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _loggerFactory))
            {
                /* Perform transactional work here */
                // DoSomething()

                compSpan.Complete();
            }
        }
    }
}

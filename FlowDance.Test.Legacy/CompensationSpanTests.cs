using System;
using FlowDance.Client;
using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Enums;
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

            using (var compSpan = new CompensationSpan(new HttpCompensatingAction("http://localhost/TripBookingService/Compensation"), traceId, _loggerFactory, CompensationSpanOption.RequiresNewBlockingCallChain))
            {
                /* Perform transactional work here */
                // DoSomething()

                compSpan.Complete();
            }
        }
    }
}

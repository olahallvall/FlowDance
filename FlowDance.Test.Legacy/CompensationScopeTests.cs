using System;
using FlowDance.Client.Legacy;
using FlowDance.Client.Legacy.RabbitMq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlowDance.Test.Legacy
{
    [TestClass]
    public class CompensationScopeTests
    {
        private static ILoggerFactory _loggerFactory;
        private static IConfigurationRoot _config;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            }); 

            _config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
        }

        [TestMethod]
        public void RootCompensationScope()
        {
            var traceId = Guid.NewGuid();

            using (CompensationScope compScope = new CompensationScope("http://localhost/TripBookingService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */
                // DoSomething()

                compScope.Complete();
            }
        }

        [TestMethod]
        public void RootWithInnerCompensationScope()
        {

            var traceId = Guid.NewGuid();

            // The top-most compensation scope is referred to as the root scope.
            // Root scope
            using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/TripBookingService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */

                // Inner scope
                using (CompensationScope compScopeInner = new CompensationScope("http://localhost/CarService/Compensation", traceId, _loggerFactory))
                {
                    /* Perform transactional work here */

                    compScopeInner.Complete();
                }
                 
                compScopeRoot.Complete();
            }
        }

        [TestMethod]
        public void RootMethodWithTwoInnerMethodCompensationScope()
        {
            var guid = Guid.NewGuid();
            RootMethod(guid);

            //Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString(), null).Count(), 4);

            //storage.DeleteStream(guid.ToString());
        }

        private void RootMethod(Guid guid)
        {
            using (CompensationScope compScope = new CompensationScope("http://localhost/HotelService/Compensation", guid, _loggerFactory))
            {
                /* Perform transactional work here */
                InnerMethod(guid);
                compScope.Complete();
            }
        }

        private void InnerMethod(Guid guid)
        {
            using (CompensationScope compScope = new CompensationScope("http://localhost/HotelService/Compensation", guid, _loggerFactory))
            {
                /* Perform transactional work here */

                compScope.Complete();
            }
        }

        [TestMethod]
        public void MultipleRootCompensationScopeUsingSameTraceId()
        {
            var traceId = Guid.NewGuid();

            // Root
            using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */

                compScopeRoot.Complete();
            }

            // Root
            using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
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

            using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
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
            using (CompensationScope compScopeRoot = new CompensationScope("http://localhost/HotelService/Compensation", traceId, _loggerFactory))
            {
                /* Perform transactional work here */

                // Inner scope 1
                using (CompensationScope compScopeInner = new CompensationScope("http://localhost/HotelService/Compensation1", traceId, _loggerFactory))
                {
                    /* Perform transactional work here */

                    compScopeInner.Complete();
                }

                // Inner scope 2
                using (CompensationScope compScopeInner = new CompensationScope("http://localhost/HotelService/Compensation2", traceId, _loggerFactory))
                {
                    /* Perform transactional work here */

                    compScopeInner.Complete();
                }

                compScopeRoot.Complete();
            }

            var storage = new Storage(_loggerFactory);
            //Assert.AreEqual(storage.ReadAllSpansFromStream(guid.ToString(), null).Count(), 6);

            //storage.DeleteStream(guid.ToString());
        }
    }


}

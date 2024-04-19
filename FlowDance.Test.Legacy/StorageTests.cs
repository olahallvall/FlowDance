using System;
using System.Linq;
using FlowDance.Client.Legacy.RabbitMQUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowDance.Test.Legacy
{
    [TestClass]
    public class StorageTests
    {
        private static ILoggerFactory _factory;
        private static IConfigurationRoot _config;

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
        public void ReadAllSpansFromStream()
        {
            var storage = new Storage(_factory);
            var spanList = storage.ReadAllSpansFromStream("c8d8070d-7680-4a70-83f1-910672af9c76");

            spanList.Count();
        }

        //[TestMethod]
        //public void CreateSystem()
        //{
        //    var storage = new Storage(_factory);
        //    storage.CreateStream(new Guid().ToString());
        //}
    }
}
using System;
using System.Linq;
using FlowDance.Client.Legacy.RabbitMQUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using RabbitMQ.Client;
using FlowDance.Client.Legacy;
using System.Threading;
using Microsoft.Testing.Platform.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace FlowDance.Test.Legacy
{
    [TestClass]
    public class StorageTests
    {
        private static ILoggerFactory _factory;
        private static IConfigurationRoot _config;
        private static Microsoft.Extensions.Logging.ILogger<StorageTests> _logger;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            _factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                // builder.AddFilter("RabbitMQ.Stream", LogLevel.Debug);
            });
            _logger = _factory.CreateLogger<StorageTests>();

            _config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
        }

        [TestMethod]
        public void ReadAllSpansFromStream()
        {
            var storage = new Storage(_factory);

            var confirmationTaskCompletionSource = new TaskCompletionSource<int>();

            _logger.LogInformation("ReadAllSpansFromStream starts - {a}", DateTime.Now.ToString("HH:mm:ss"));
            var spanList = storage.ReadAllSpansFromStream("c8d8070d-7680-4a70-83f1-910672af9c76", confirmationTaskCompletionSource);

            // Wait for confirmation feedback 
            confirmationTaskCompletionSource.Task.Wait();
            _logger.LogInformation("ReadAllSpansFromStream ends - {a}", DateTime.Now.ToString("HH:mm:ss"));

            Assert.AreEqual(spanList.Count(), 2);
        }

        [TestMethod]
        public void ReadAllSpansFromStream2()
        {
            var messageListener = new MessageListener(_factory);
            CancellationToken ct = new CancellationToken(false);

            messageListener.StartAsync(ct); //.GetAwaiter().GetResult();

            // messageListener.StopAsync(ct);



        }

        //[TestMethod]
        //public void ReadAllSpansFromStream2()
        //{
        //    var storage = new Storage(_factory);

        //    var spanList = storage.ReadRabbitMsg("c8d8070d-7680-4a70-83f1-910672af9c76");


        //}
    }
}
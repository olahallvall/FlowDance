using RabbitMQ.Stream.Client;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace FlowDance.Common.RabbitMQUtils
{
    internal class SingletonStreamSystem
    {
        private static ILogger<StreamSystem>? _logger;
        private static readonly SingletonStreamSystem Instance = new();
        private StreamSystem? _streamSystem;
       
        private SingletonStreamSystem()
        {
        }

        public static SingletonStreamSystem GetInstance(ILogger<StreamSystem> logger)
        {
            _logger = logger;
            return Instance;
        }

        public StreamSystem GetStreamSystem()
        {
            if(_streamSystem == null)
            {
                var sw = new Stopwatch();
                sw.Start();

                var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();

                _streamSystem = StreamSystem.Create(new StreamSystemConfig()
                {
                    UserName = config.GetSection("RabbitMqConnection").GetSection("Username").Value,
                    Password = config.GetSection("RabbitMqConnection").GetSection("Password").Value,
                    Endpoints = new List<EndPoint>() { new IPEndPoint(IPAddress.Loopback, 5552) }
                }, _logger).GetAwaiter().GetResult();

                sw.Stop();
                _logger.LogInformation("A call to GetStreamSystem created a new StreamSystem in {0} ms.", sw.Elapsed.TotalMilliseconds);
            }

            _logger.LogInformation("A call to GetStreamSystem returns an existing StreamSystem.");
            return _streamSystem;
        }
    }
}

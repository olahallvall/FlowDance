using RabbitMQ.Stream.Client;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace FlowDance.Common.RabbitMQUtils
{
    internal class SingletonStreamSystem
    {
        private static ILogger<StreamSystem>? _logger;
        private static SingletonStreamSystem _instance = new SingletonStreamSystem();
        private StreamSystem? _streamSystem;
       
        private SingletonStreamSystem()
        {
        }

        public static SingletonStreamSystem GetInstance(ILogger<StreamSystem> logger)
        {
            _logger = logger;
            return _instance;
        }

        public StreamSystem GetStreamSystem()
        {
            if(_streamSystem == null)
            {
                var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();

                _streamSystem = StreamSystem.Create(new StreamSystemConfig()
                {
                    UserName = config.GetSection("RabbitMqConnection").GetSection("Username").Value,
                    Password = config.GetSection("RabbitMqConnection").GetSection("Password").Value,
                    Endpoints = new List<EndPoint>() { new IPEndPoint(IPAddress.Loopback, 5552) }
                }, _logger).Result;
            }
            return _streamSystem;
        }
    }
}

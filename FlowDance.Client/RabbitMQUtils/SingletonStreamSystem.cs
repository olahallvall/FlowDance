using RabbitMQ.Stream.Client;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace FlowDance.Client.RabbitMQUtils
{
    internal class SingletonStreamSystem
    {
        private static ILogger<StreamSystem>? _logger;
        private static SingletonStreamSystem INSTANCE = new SingletonStreamSystem();
        private StreamSystem? _streamSystem;
       
        private SingletonStreamSystem()
        {
        }

        public static SingletonStreamSystem getInstance(ILogger<StreamSystem> logger)
        {
            _logger = logger;
            return INSTANCE;
        }

        public StreamSystem getStreamSystem()
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

using RabbitMQ.Stream.Client;
using System.Net;
using Microsoft.Extensions.Logging;

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
                _streamSystem = StreamSystem.Create(new StreamSystemConfig()
                {
                    UserName = "guest",
                    Password = "guest",
                    Endpoints = new List<EndPoint>() { new IPEndPoint(IPAddress.Loopback, 5552) }
                }, _logger).Result;
            }
            return _streamSystem;
        }
    }
}

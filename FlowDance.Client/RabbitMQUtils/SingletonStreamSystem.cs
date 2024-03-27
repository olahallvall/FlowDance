using RabbitMQ.Stream.Client; 
using RabbitMQ.Stream.Client.Reliable;

namespace FlowDance.Client.RabbitMQUtils
{
    internal class SingletonStreamSystem
    {
        private static SingletonStreamSystem INSTANCE = new SingletonStreamSystem();
        private StreamSystem streamSystem;
      
        private SingletonStreamSystem()
        {
            streamSystem = StreamSystem.Create(new StreamSystemConfig() 
            {
                UserName = "guest",
                Password = "guest",
                Endpoints = new List<EndPoint>() {new IPEndPoint(IPAddress.Loopback, 5552)}
            }, null).ConfigureAwait(false);
        }

        public static SingletonStreamSystem getInstance()
        {
            return INSTANCE;
        }

        public StreamSystem getStreamSystem()
        {
            return streamSystem;
        }
    }
}

using RabbitMQ.Client;

namespace FlowDance.Client.RabbitMQUtils
{
    internal class SingletonConnection
    {
        private static SingletonConnection INSTANCE = new SingletonConnection();
        private IConnection connection;
        private SingletonConnection()
        {
            // here you can init your connection parameter
            var factory = new ConnectionFactory { HostName = "localhost" };
            connection = factory.CreateConnection();
        }

        public static SingletonConnection getInstance()
        {
            return INSTANCE;
        }

        public IConnection getConnection()
        {
            return connection;
        }
    }
}

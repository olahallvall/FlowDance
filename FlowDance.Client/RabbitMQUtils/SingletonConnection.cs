using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace FlowDance.Client.RabbitMQUtils
{
    internal class SingletonConnection
    {
        private static SingletonConnection INSTANCE = new SingletonConnection();
        private IConnection connection;
        private SingletonConnection()
        {
            var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
            var connectionFactory = new ConnectionFactory();
            config.GetSection("RabbitMqConnection").Bind(connectionFactory);

            connection = connectionFactory.CreateConnection();
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

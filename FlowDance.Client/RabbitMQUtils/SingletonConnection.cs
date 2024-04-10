using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace FlowDance.Client.RabbitMQUtils
{
    internal class SingletonConnection
    {
        private static SingletonConnection _instance = new SingletonConnection();
        private IConnection _connection;
        private SingletonConnection()
        {
            var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
            var connectionFactory = new ConnectionFactory();
            config.GetSection("RabbitMqConnection").Bind(connectionFactory);

            _connection = connectionFactory.CreateConnection();
        }

        public static SingletonConnection GetInstance()
        {
            return _instance;
        }

        public IConnection GetConnection()
        {
            return _connection;
        }
    }
}

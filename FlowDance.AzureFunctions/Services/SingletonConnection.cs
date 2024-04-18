using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Diagnostics;

namespace FlowDance.AzureFunctions.Services
{
    internal class SingletonConnection
    {
        private static SingletonConnection _instance = new SingletonConnection();
        private IConnection _connection;
        private SingletonConnection()
        {
            var sw = new Stopwatch();
            sw.Start();

            var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
            var connectionFactory = new ConnectionFactory();
            config.GetSection("RabbitMqConnection").Bind(connectionFactory);

            _connection = connectionFactory.CreateConnection();

            sw.Stop();
            Console.WriteLine("The constructor in SingletonConnection created a new (RabbitMQ) IConnection in {0} ms.", sw.Elapsed.TotalMilliseconds);
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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using RabbitMQ.Stream.Client;
using FlowDance.Common.Commands;
using Newtonsoft.Json;
using System.Text;

namespace FlowDance.Server.Services
{
    public interface IStorageQueueService
    {
        public SpanCommand StoreCommand(SpanCommand spanCommand);
    }

    public class StorageQueueService : IStorageQueueService
    {
        private readonly ILogger<StorageQueueService> _logger;
        private readonly IConfiguration _configuration;

        public StorageQueueService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<StorageQueueService>();
            _configuration = configuration;
        }

        /// <summary>
        /// Store commands to a queue.
        /// </summary>
        /// <param name="spanCommand"></param>
        /// <returns></returns>
        public SpanCommand StoreCommand(SpanCommand spanCommand)
        {
            try
            {
                var connectionFactory = new ConnectionFactory();
                connectionFactory.Uri = new Uri(_configuration["RabbitMq_Connection"]);

                var connection = connectionFactory.CreateConnection();
                var channel = connection.CreateModel();

                channel.ConfirmSelect();

                channel.QueueDeclare(queue: "FlowDance.SpanCommands",
                    durable: true,
                exclusive: false,
                autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(spanCommand));

                channel.BasicPublish(exchange: string.Empty,
                    routingKey: "FlowDance.SpanCommands",
                    basicProperties: null,
                    body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(spanCommand, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));

                channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                if (connection.IsOpen)
                    connection.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can't store command to a queue. TraceId:{TraceId}", spanCommand.TraceId.ToString());
                throw;
            }

            return spanCommand;
        }
    }
}

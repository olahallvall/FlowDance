using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using FlowDance.Common.Commands;
using Newtonsoft.Json;
using System.Text;

namespace FlowDance.Server.Services
{
    public interface IStorageQueueService
    {
        public Task<SpanCommand> StoreCommandAsync(SpanCommand spanCommand);
    }

    public class StorageQueueService : IStorageQueueService
    {
        private readonly ILogger<StorageQueueService> _logger;
        private readonly IConfiguration _configuration;
        private readonly CreateChannelOptions _channelOpts;
        private const ushort MAX_OUTSTANDING_CONFIRMS = 256;
        public StorageQueueService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<StorageQueueService>();
            _configuration = configuration;

            _channelOpts = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true,
                    outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(MAX_OUTSTANDING_CONFIRMS)
             );
        }

        /// <summary>
        /// Store commands to a queue.
        /// </summary>
        /// <param name="spanCommand"></param>
        /// <returns></returns>
        public async Task<SpanCommand> StoreCommandAsync(SpanCommand spanCommand)
        {
            try
            {
                var connectionFactory = new ConnectionFactory();
                connectionFactory.Uri = new Uri(_configuration["RabbitMq_Connection"]!);

                var connection = await connectionFactory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync(_channelOpts);

                await channel.QueueDeclareAsync(queue: "FlowDance.SpanCommands",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(spanCommand));

                await channel.BasicPublishAsync(exchange: string.Empty,
                    routingKey: "FlowDance.SpanCommands",
                    mandatory: true,
                    basicProperties: new BasicProperties { Persistent = true },
                    body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(spanCommand, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));

                if (connection.IsOpen)
                    await connection.CloseAsync();
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

using FlowDance.Common.Commands;
using FlowDance.Common.Events;
using FlowDance.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FlowDance.Client.StorageProviders
{
    /// <summary>
    /// Handles the storing of events and messages to RabbitMQ. 
    /// </summary>
    public class RabbitMqStorage : IStorageProvider
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<RabbitMqStorage> _logger;
        private readonly CreateChannelOptions _channelOpts;
        private const ushort MAX_OUTSTANDING_CONFIRMS = 256;
        private readonly IConfigurationRoot _configuration;
        public RabbitMqStorage(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<RabbitMqStorage>();

            _channelOpts = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true,
                    outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(MAX_OUTSTANDING_CONFIRMS)
             );

            _configuration = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
        }

        /// <summary>
        /// Store Events to a stream.
        /// </summary>
        /// <param name="spanEvent"></param>
        /// <exception cref="Exception"></exception>
        public async Task<SpanEvent> StoreEventInStreamAsync(SpanEvent spanEvent)
        {
            try
            {
                var connectionFactory = new ConnectionFactory();
                _configuration.GetSection("RabbitMqConnection").Bind(connectionFactory);
                var connection = await connectionFactory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync(_channelOpts);

                var streamName = spanEvent.TraceId.ToString();

                //Check if stream/queue exist. 
                if (await StreamExistOrQueue(streamName, connection))
                {
                    // Only first spanEvent in stream should be a root spanEvent.
                    if (spanEvent is SpanOpened)
                        ((SpanOpened)spanEvent).IsRootSpan = false;

                    // Store the messages
                    await channel.BasicPublishAsync(exchange: string.Empty,
                            routingKey: streamName,
                            mandatory: true,
                            basicProperties: new BasicProperties { Persistent = true },
                            body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(spanEvent, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));
                }
                else // Stream don´t exists.
                {
                    // SpanClosed should newer create the CreateQueue. Only SpanEventOpened are allowed to do that!  
                    if (spanEvent is SpanClosed)
                        throw new Exception("The event SpanClosed are trying to create a stream for the first time. This not allowed, only SpanEventOpened are allowed to do that!");

                    if (spanEvent is SpanOpened)
                        ((SpanOpened)spanEvent).IsRootSpan = true;

                    // Create stream
                    CreateStream(streamName, channel);

                    // Store the messages
                    await channel.BasicPublishAsync(exchange: string.Empty,
                            routingKey: streamName,
                            mandatory: true,
                            basicProperties: new BasicProperties { Persistent = true },
                            body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(spanEvent, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));

                    if (connection.IsOpen)
                        await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can't store event to a stream. TraceId:{TraceId}", spanEvent.TraceId.ToString());
                throw;
            }

            return spanEvent;
        }

        /// <summary>
        /// Store Events to a queue.
        /// </summary>
        /// <param name="spanEvent"></param>
        public async Task<SpanEvent> StoreEventInQueueAsync(SpanEvent spanEvent)
        {
            try
            {
                var connectionFactory = new ConnectionFactory();
                _configuration.GetSection("RabbitMqConnection").Bind(connectionFactory);
                var connection = await connectionFactory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync(_channelOpts);

                await channel.QueueDeclareAsync(queue: "FlowDance.SpanEvents",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(spanEvent));

                // Store the messages
                await channel.BasicPublishAsync(exchange: string.Empty,
                        routingKey: "FlowDance.SpanEvents",
                        mandatory: true,
                        basicProperties: new BasicProperties { Persistent = true },
                        body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(spanEvent, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));

                if (connection.IsOpen)
                    await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can't store a event to a queue. TraceId:{TraceId}", spanEvent.TraceId.ToString());
                throw;
            }

            return spanEvent;
        }

        /// <summary>
        /// Store commands to a queue.
        /// </summary>
        /// <param name="spanCommand"></param>
        public async Task<SpanCommand> StoreCommandAsync(SpanCommand spanCommand)
        {
            try
            {
                var connectionFactory = new ConnectionFactory();
                _configuration.GetSection("RabbitMqConnection").Bind(connectionFactory);
                var connection = await connectionFactory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync(_channelOpts);

                //channel.ConfirmSelect();

                await channel.QueueDeclareAsync(queue: "FlowDance.SpanCommands",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(spanCommand));
   
                // Store the messages
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

        /// <summary>
        /// Check if a queue/stream exists. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>True if stream exists, else false.</returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> StreamExistOrQueue(string name, IConnection connection)
        {
            try
            {
                var channel = await connection.CreateChannelAsync();
                QueueDeclareOk ok = await channel.QueueDeclarePassiveAsync(name);
            }
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
            {
                if (ex.Message.Contains("no queue"))
                    return false;
                else
                    throw new Exception("A suspected exception occurred. See inner exception for more details.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The StreamExistOrQueue function returns error when checking existens of queue:{name}", name);
                throw new Exception("A suspected exception occurred. See inner exception for more details.", ex);
            }

            return true;
        }

        /// <summary>
        /// Create a stream. 
        /// </summary>
        /// <param name="streamName"></param>
        private void CreateStream(string streamName, IChannel channel)
        {
            try
            {
                channel.QueueDeclareAsync(queue: streamName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object> { { "x-queue-type", "stream" } }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The CreateStream function returns error when creating a stream with the name:{streamName}", streamName);
                throw;
            }   
        }
    }
}
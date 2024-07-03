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

namespace FlowDance.Client.StorageProviders
{
    /// <summary>
    /// Handles the storing of events and messages to RabbitMQ. 
    /// </summary>
    public class RabbitMqStorage : IStorageProvider
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<RabbitMqStorage> _logger;

        public RabbitMqStorage(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<RabbitMqStorage>();
        }

        /// <summary>
        /// Store Events to a stream.
        /// </summary>
        /// <param name="spanEvent"></param>
        /// <exception cref="Exception"></exception>
        public SpanEvent StoreEventInStream(SpanEvent spanEvent)
        {
            try
            {
                var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
                var connectionFactory = new ConnectionFactory();
                config.GetSection("RabbitMqConnection").Bind(connectionFactory);

                var connection = connectionFactory.CreateConnection();
                var channel = connection.CreateModel();

                var streamName = spanEvent.TraceId.ToString();

                //Check if stream/queue exist. 
                if (StreamExistOrQueue(streamName, connection))
                {
                    // Only first spanEvent in stream should be a root spanEvent.
                    if (spanEvent is SpanOpened)
                        ((SpanOpened)spanEvent).IsRootSpan = false;

                    // So we can Confirm
                    channel.ConfirmSelect();

                    // Store the messages
                    channel.BasicPublish(exchange: string.Empty,
                            routingKey: streamName,
                            basicProperties: null,
                            body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(spanEvent, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));

                    channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
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

                    // So we can Confirm
                    channel.ConfirmSelect();

                    // Store the messages
                    channel.BasicPublish(exchange: string.Empty,
                        routingKey: streamName,
                        basicProperties: null,
                        body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(spanEvent, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));

                    channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                    if (connection.IsOpen)
                        connection.Close();
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
        public SpanEvent StoreEventInQueue(SpanEvent spanEvent)
        {
            try
            {
                var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
                var connectionFactory = new ConnectionFactory();
                config.GetSection("RabbitMqConnection").Bind(connectionFactory);

                var connection = connectionFactory.CreateConnection();
                var channel = connection.CreateModel();

                channel.ConfirmSelect();

                channel.QueueDeclare(queue: "FlowDance.SpanEvents",
                    durable: true,
                exclusive: false,
                autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(spanEvent));

                channel.BasicPublish(exchange: string.Empty,
                    routingKey: "FlowDance.SpanEvents",
                    basicProperties: null,
                    body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(spanEvent, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));

                channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                if (connection.IsOpen)
                    connection.Close();
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
        public SpanCommand StoreCommand(SpanCommand spanCommand)
        {
            try
            {
                var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
                var connectionFactory = new ConnectionFactory();
                config.GetSection("RabbitMqConnection").Bind(connectionFactory);

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

        /// <summary>
        /// Check if a queue/stream exists. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>True if stream exists, else false.</returns>
        /// <exception cref="Exception"></exception>
        private bool StreamExistOrQueue(string name, IConnection connection)
        {
            try
            {
                var channel = connection.CreateModel();
                QueueDeclareOk ok = channel.QueueDeclarePassive(name);
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
        private void CreateStream(string streamName, IModel channel)
        {
            try
            {
                channel.QueueDeclare(queue: streamName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object> { { "x-queue-type", "stream" } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The CreateStream function returns error when creating a stream with the name:{streamName}", streamName);
                throw;
            }   
        }
    }
}
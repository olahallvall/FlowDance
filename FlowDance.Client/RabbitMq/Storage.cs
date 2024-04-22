using System;
using System.Collections.Generic;
using System.Text;
using FlowDance.Common.Commands;
using FlowDance.Common.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace FlowDance.Client.RabbitMq
{
    /// <summary>
    /// This class handles the reading and storing of messages to RabbitMQ. 
    /// </summary>
    public class Storage
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<Storage> _logger;

        public Storage(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<Storage>();
        }

        public void StoreEvent(Span span, IConnection connection, IModel channel)
        {
            var streamName = span.TraceId.ToString();

            //Check if stream/queue exist. 
            if (StreamExistOrQueue(streamName, connection))   
            {
                // Only first span in stream should be a root span.
                if (span is SpanOpened)
                    ((SpanOpened)span).IsRootSpan = false;

                // So we can Confirm
                channel.ConfirmSelect();

                // Store the messages
                channel.BasicPublish(exchange: string.Empty,
                        routingKey: streamName,
                        basicProperties: null,
                        body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(span, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));

                channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            }
            else // Stream don´t exists.
            {
                // SpanClosed should newer create the CreateQueue. Only SpanOpened are allowed to do that!  
                if (span is SpanClosed)
                    throw new Exception("The event SpanClosed are trying to create a stream for the first time. This not allowed, only SpanOpened are allowed to do that!");

                if (span is SpanOpened)
                    ((SpanOpened)span).IsRootSpan = true;

                // Create stream
                CreateStream(streamName, channel);

                // So we can Confirm
                channel.ConfirmSelect();

                // Store the messages
                channel.BasicPublish(exchange: string.Empty,
                    routingKey: streamName,
                    basicProperties: null,
                    body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(span, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));

                channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            }
        }

        public void StoreCommand(DetermineCompensation command, IModel channel)
        {
            channel.ConfirmSelect();

            channel.QueueDeclare(queue: "FlowDance.DetermineCompensation",
                durable: true,
            exclusive: false,
            autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command));

            channel.BasicPublish(exchange: string.Empty,
                routingKey: "FlowDance.DetermineCompensation",
                basicProperties: null,
                body: body);

            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Check if a queue/stream exists. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="connection"></param>
        /// <returns>True if stream exists, else false.</returns>
        /// <exception cref="Exception"></exception>
        public bool StreamExistOrQueue(string name, IConnection connection)
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
                    throw new Exception("Non suspected exception occurred. See inner exception for more details.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Non suspected exception occurred. See inner exception for more details.", ex);
            }

            return true;
        }

        /// <summary>
        /// Create a stream. 
        /// </summary>
        /// <param name="streamName"></param>
        /// <param name="channel"></param>
        public void CreateStream(string streamName, IModel channel)
        {
            channel.QueueDeclare(queue: streamName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object> { { "x-queue-type", "stream" } });
        }
    }
}
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
    /// Handles the storing of events and messages to RabbitMQ. 
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

        /// <summary>
        /// Store a SpanEvent (SpanOpened or SpanClosed) to a stream.
        /// </summary>
        /// <param name="spanEvent"></param>
        /// <param name="connection"></param>
        /// <param name="channel"></param>
        /// <exception cref="Exception"></exception>
        public void StoreEvent(SpanEvent spanEvent, IConnection connection, IModel channel)
        {
            try
            {
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can't store event to a stream. TraceId:{TraceId}", spanEvent.TraceId.ToString());
                throw;
            }
        }

        /// <summary>
        /// Store the DetermineCompensation command to a queue.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="channel"></param>
        public void StoreCommand(DetermineCompensation command, IModel channel)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Can't store DetermineCompensation to a queue. TraceId:{TraceId}", command.TraceId.ToString());
                throw;
            }
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
                _logger.LogError(ex, "The StreamExistOrQueue function returns error when checking existens of queue:{name}", name);
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
using FlowDance.Common.Events;
using FlowDance.Common.Commands;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using RabbitMQ.Client.Events;
using System.Threading.Channels;

namespace FlowDance.Client.Legacy.RabbitMQUtils
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

        public void StoreEvent(Span span, IModel channel)
        {
            try
            {
                var streamName = span.TraceId.ToString();
                //var channel = SingletonConnection.GetInstance().GetConnection().CreateModel();

                //Check if stream/queue exist. 
                uint messageCount = 0;
                if (StreamExistOrQueue(streamName, ref messageCount))
                {
                    var confirmationTaskCompletionSource = new TaskCompletionSource<int>();

                    // Only first span in stream should be a root span.
                    if (span is SpanOpened)
                        ((SpanOpened)span).IsRootSpan = false;

                    //var spanList = ReadAllSpansFromStream(span.TraceId.ToString(), confirmationTaskCompletionSource);
                    // Wait for confirmation feedback 
                    //confirmationTaskCompletionSource.Task.Wait();

                    // Validate against previous events grouped by the same TraceId. 
                    //ValidateStoredSpans(spanList);

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
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void StoreCommand(DetermineCompensation command, IModel channel)
        {
            //var channel = SingletonConnection.GetInstance().GetConnection().CreateModel();
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

            channel.Close();
        }

        public List<Span> ReadRabbitMsg(string queue)
        {
            // https://www.rabbitmq.com/client-libraries/dotnet-api-guide
            var spanList = new List<Span>();
            var channel = SingletonConnection.GetInstance().GetConnection().CreateModel();

            if (channel.MessageCount(queue) == 0) return null;
            BasicGetResult result = channel.BasicGet(queue, true);
            if (result == null) return null;
            else
            {
                IBasicProperties props = result.BasicProperties;
                var messageContent = JsonConvert.DeserializeObject<Span>(Encoding.UTF8.GetString(result.Body.ToArray()), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
                    if (messageContent != null) spanList.Add(messageContent);

                channel.BasicAck(result.DeliveryTag, false);
            }

            return spanList;
        }

        public List<Span> ReadAllSpansFromStream(string streamName, TaskCompletionSource<int> taskCompletionSource)
        {
            var spanList = new List<Span>();
            try
            {
                var channel = SingletonConnection.GetInstance().GetConnection().CreateModel();
                uint numberOfMessages = 0;

                if (StreamExistOrQueue(streamName, ref numberOfMessages))
                {
                    if (numberOfMessages > 0)
                    {
                        var numberOfMessageReceived = 0;
                        channel.BasicQos(0, 100, false);
                        string consumerTag = "";

                        // Setup the Channel
                        var consumer = new EventingBasicConsumer(channel);

                        // Received
                        void OnConsumerOnReceived(object model, BasicDeliverEventArgs ea)
                        {
                            _logger.LogInformation("OnConsumerOnReceived");

                            var messageContent = JsonConvert.DeserializeObject<Span>(Encoding.UTF8.GetString(ea.Body.ToArray()), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
                            if (messageContent != null) spanList.Add(messageContent);

                            numberOfMessageReceived++;
                            if (numberOfMessageReceived == (int)numberOfMessages)
                            {
                                channel.BasicCancel(consumerTag);
                                taskCompletionSource.SetResult(numberOfMessageReceived);
                                _logger.LogInformation("Got all message - {numberOfMessageReceived}", numberOfMessageReceived);
                            }
                        }

                        consumer.Received += OnConsumerOnReceived;

                        // Start consuming...
                        consumerTag = channel.BasicConsume(queue: streamName, consumer: consumer, autoAck: false, arguments: new Dictionary<string, object> { { "x-stream-offset", 0 } });
                    }
                }
            }
            catch (Exception e)
            {
                    Console.WriteLine(e);
                    throw;
            }
            
            if(taskCompletionSource != null)
                taskCompletionSource.Task.Wait();

            return spanList;
        }

        private void ValidateStoredSpans(List<Span> spanList)
        {
            if (spanList.Any())
            {
                // Rule #1 - Can´t add Span after the root Span has been closed.
                var spanOpened = spanList[0];
                var spanClosed = from s in spanList
                                 where s.SpanId == spanOpened.SpanId && s.GetType() == typeof(SpanClosed)
                                 select s;

                if (spanClosed.Any())
                    throw new Exception("Spans can´t be add after the root Span has been closed");
            }
        }

        /// <summary>
        /// Check if a queue/stream exists. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="channel"></param>
        /// <param name="messageCount"></param>
        /// <returns>True if stream exists, else false.</returns>
        /// <exception cref="Exception"></exception>
        public bool StreamExistOrQueue(string name, ref uint messageCount)
        {
            try
            {
                var channel = SingletonConnection.GetInstance().GetConnection().CreateModel();
                QueueDeclareOk ok = channel.QueueDeclarePassive(name);
                messageCount = ok.MessageCount;
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

        /// <summary>
        /// Delete a stream. 
        /// </summary>
        /// <param name="streamName"></param>
        public void DeleteStream(string streamName)
        {
            SingletonConnection.GetInstance().GetConnection().CreateModel().QueueDelete(streamName);
        }
    }
}
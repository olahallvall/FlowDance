using FlowDance.Common.Events;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace FlowDance.Client.CLI.StorageProviders
{
    public class Storage 
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<Consumer> _consumerLogger;
        private readonly ILogger<StreamSystem> _streamLogger;

        public Storage(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            _consumerLogger = _loggerFactory.CreateLogger<Consumer>();
            _streamLogger = _loggerFactory.CreateLogger<StreamSystem>();
        }

        public List<SpanEvent> ReadAllSpanEventsFromStream(string streamName)
        {
            var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
            var streamSystem = StreamSystem.Create(new StreamSystemConfig()
            {
                UserName = config.GetSection("RabbitMqConnection").GetSection("Username").Value,
                Password = config.GetSection("RabbitMqConnection").GetSection("Password").Value,
                Endpoints = new List<EndPoint>() { new IPEndPoint(IPAddress.Loopback, 5552) }
            }, _streamLogger).GetAwaiter().GetResult();

            return ReadAllSpansFromStream(streamName, streamSystem, _consumerLogger);
        }

        /// <summary>
        /// Get the Offset-number from the last massage in the stream. If stream is empty the out variable emptyStream sets to true.  
        /// </summary>
        /// <param name="streamName"></param>
        /// <param name="streamSystem"></param>
        /// <param name="emptyStream"></param>
        /// <param name="consumerLogger"></param>
        /// <returns></returns>
        private ulong GetLastOffset(string streamName, StreamSystem streamSystem, out bool emptyStream, ILogger<Consumer> consumerLogger)
        {
            var consumerTaskCompletionSource = new TaskCompletionSource<int>();
            ulong lastOffset = 0;
            var timeout = new TimeSpan(0, 0, 5);

            var consumer = Consumer.Create(
                    new ConsumerConfig(streamSystem, streamName)
                    {
                        OffsetSpec = new OffsetTypeLast(),
                        ClientProvidedName = "FlowDance.Client.Consumer",
                        MessageHandler = async (stream, consumer, context, message) =>
                        {
                            lastOffset = context.Offset;
                            consumerTaskCompletionSource.SetResult(1);
                            await Task.CompletedTask;
                        }
                    }, consumerLogger).GetAwaiter().GetResult();

            consumerTaskCompletionSource.Task.Wait(timeout);
            consumer.Close();

            if (!consumerTaskCompletionSource.Task.IsCompleted)
                emptyStream = true;
            else
                emptyStream = false;

            return lastOffset;
        }

        /// <summary>
        /// Reads all messages from the stream.
        /// </summary>
        /// <param name="streamName"></param>
        /// <param name="streamSystem"></param>
        /// <param name="consumerLogger"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private List<SpanEvent> ReadAllSpansFromStream(string streamName, StreamSystem streamSystem, ILogger<Consumer> consumerLogger)
        {
            var lastOffset = GetLastOffset(streamName, streamSystem, out bool emptyStream, consumerLogger);
            var spanEventList = new List<SpanEvent>();

            if (emptyStream)
                return spanEventList;

            if (lastOffset >= 0)
            {
                var consumerTaskCompletionSource = new TaskCompletionSource<int>();
                int currentOffset = 0;

                var consumer = Consumer.Create(
                        new ConsumerConfig(streamSystem, streamName)
                        {
                            OffsetSpec = new OffsetTypeFirst(),
                            ClientProvidedName = "FlowDance.AzureFunctions.Consumer",
                            MessageHandler = async (stream, consumer, context, message) =>
                            {
                                var messageContent = JsonConvert.DeserializeObject<SpanEvent>(Encoding.UTF8.GetString(message.Data.Contents), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
                                if (messageContent != null)
                                    spanEventList.Add(messageContent);

                                if (currentOffset == (int)lastOffset)
                                    consumerTaskCompletionSource.SetResult(1);

                                currentOffset++;

                                await Task.CompletedTask;
                            }
                        }, consumerLogger).GetAwaiter().GetResult();

                consumerTaskCompletionSource.Task.Wait();
                consumer.Close();
            }
            return spanEventList;
        }
    }
}

using FlowDance.Common.Events;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Net;
using RabbitMQ.Client;

namespace FlowDance.Server.Services;

public interface IStorageService
{
    public List<SpanEvent> ReadAllSpanEventsFromStream(string streamName);
}

/// <summary>
///This class handles the reading and storing of messages to RabbitMQ. 
/// 
/// Based on code from this site - https://rabbitmq.github.io/rabbitmq-stream-dotnet-client/stable/htmlsingle/index.html
/// </summary>
public class StorageService : IStorageService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Consumer> _consumerLogger;
    private readonly ILogger<StreamSystem> _streamLogger;
    private readonly IConfiguration _configuration;
    private StreamSystem? _streamSystem;

    public StorageService(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
        _consumerLogger = _loggerFactory.CreateLogger<Consumer>();
        _streamLogger = _loggerFactory.CreateLogger<StreamSystem>();

        // The way to connect comes from this repo -
        // https://github.com/rabbitmq/rabbitmq-stream-dotnet-client/tree/main/docs/ReliableClient

        var connectionFactory = new ConnectionFactory();
        connectionFactory.Uri = new Uri(_configuration["RabbitMq_Connection"]);

        var ep = new IPEndPoint(IPAddress.Loopback, 5552);

        var streamPort = Int32.Parse(_configuration["RabbitMq_StreamSettings:StreamPort"]);
        var loadBalancer = bool.Parse(_configuration["RabbitMq_StreamSettings:LoadBalancer"]);

        if (connectionFactory.HostName != "localhost")
        {
            switch (Uri.CheckHostName(connectionFactory.HostName))
            {
                case UriHostNameType.IPv4:
                    if (connectionFactory.HostName != null) ep = new IPEndPoint(IPAddress.Parse(connectionFactory.HostName), streamPort);
                    break;
                case UriHostNameType.Dns:
                    if (connectionFactory.HostName != null)
                    {
                        var addresses = Dns.GetHostAddresses(connectionFactory.HostName);
                        ep = new IPEndPoint(addresses[0], streamPort);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var streamSystemConfig = new StreamSystemConfig()
        {
            UserName = connectionFactory.UserName,
            Password = connectionFactory.Password,
            VirtualHost = connectionFactory.VirtualHost,
            Endpoints = new List<EndPoint>() { ep }
        };

        if (loadBalancer)
        {
            var resolver = new AddressResolver(ep);
            streamSystemConfig = new StreamSystemConfig()
            {
                AddressResolver = resolver,
                UserName = connectionFactory.UserName,
                Password = connectionFactory.Password,
                VirtualHost = connectionFactory.VirtualHost,
                Endpoints = new List<EndPoint>() { resolver.EndPoint }
            };
        }

        _streamSystem = StreamSystem.Create(streamSystemConfig, _streamLogger).GetAwaiter().GetResult();
    }

    public List<SpanEvent> ReadAllSpanEventsFromStream(string streamName)
    {
        return ReadAllSpansFromStream(streamName, _consumerLogger);
    }

    /// <summary>
    /// Get the Offset-number from the last massage in the stream. If stream is empty the out variable emptyStream sets to true.  
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="emptyStream"></param>
    /// <param name="consumerLogger"></param>
    /// <returns></returns>
    private ulong GetLastOffset(string streamName, out bool emptyStream, ILogger<Consumer> consumerLogger)
    {
        var consumerTaskCompletionSource = new TaskCompletionSource<int>();
        ulong lastOffset = 0;
        var timeout = new TimeSpan(0, 0, 5);

        var consumer = Consumer.Create(
                new ConsumerConfig(_streamSystem, streamName)
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
    /// <param name="consumerLogger"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private List<SpanEvent> ReadAllSpansFromStream(string streamName, ILogger<Consumer> consumerLogger)
    {
        var lastOffset = GetLastOffset(streamName, out bool emptyStream, consumerLogger);
        var spanEventList = new List<SpanEvent>();

        if (emptyStream)
            return spanEventList;

        if (lastOffset >= 0)
        {
            var consumerTaskCompletionSource = new TaskCompletionSource<int>();
            int currentOffset = 0;

            var consumer = Consumer.Create(
                    new ConsumerConfig(_streamSystem, streamName)
                    {
                        OffsetSpec = new OffsetTypeFirst(),
                        ClientProvidedName = "FlowDance.Server.Consumer",
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

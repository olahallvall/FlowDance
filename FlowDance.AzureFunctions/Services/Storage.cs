using FlowDance.Common.Events;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;

namespace FlowDance.AzureFunctions.Services;

/// <summary>
///This class handles the reading and storing of messages to RabbitMQ. 
/// 
/// Based on code from this site - https://rabbitmq.github.io/rabbitmq-stream-dotnet-client/stable/htmlsingle/index.html
/// </summary>
public class Storage : IStorage
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
        return ReadAllSpansFromStream(streamName, _consumerLogger);
    }

    /// <summary>
    /// Get the Offset-number from the last massage in the stream.
    /// Use this method very carefully!!! The stream needs to have at least one message. If not this method will wait until one message arrives.
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="consumerLogger"></param>
    /// <returns></returns>
    private ulong GetLastOffset(string streamName, ILogger<Consumer> consumerLogger)
    {
        var consumerTaskCompletionSource = new TaskCompletionSource<int>();
        var streamSystem = SingletonStreamSystem.GetInstance(_streamLogger).GetStreamSystem();
        ulong lastOffset = 0;

        // https://stackoverflow.com/questions/67267967/timeout-and-stop-a-task
        // https://stackoverflow.com/questions/22637642/using-cancellationtoken-for-timeout-in-task-run-does-not-work

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

        // Todo: Add support for CancellationTokenSource and Timer - wait max 5 sec
        consumerTaskCompletionSource.Task.Wait();

        consumer.Close();

        return lastOffset;
    }

    /// <summary>
    /// Reads all messages from the stream.
    /// Use this method very carefully!!! The stream needs to have at least one message. If not this method will wait until one message arrives.
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="consumerLogger"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private List<SpanEvent> ReadAllSpansFromStream(string streamName, ILogger<Consumer> consumerLogger)
    {
        var numberOfMessages = GetLastOffset(streamName, consumerLogger) + 1;
        var spanEventList = new List<SpanEvent>();

        if (numberOfMessages > 0)
        {
            var consumerTaskCompletionSource = new TaskCompletionSource<int>();
            var streamSystem = SingletonStreamSystem.GetInstance(_streamLogger).GetStreamSystem();
            int numberOfMessageReceived = 0;

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

                            numberOfMessageReceived++;

                            if (numberOfMessageReceived == (int)numberOfMessages)
                                consumerTaskCompletionSource.SetResult(1);

                            await Task.CompletedTask;
                        }
                    }, consumerLogger).GetAwaiter().GetResult();

            // Todo: Add support for CancellationTokenSource and Timer - wait max 5 sec
            consumerTaskCompletionSource.Task.Wait();

            consumer.Close();
        }

        return spanEventList;
    }
}

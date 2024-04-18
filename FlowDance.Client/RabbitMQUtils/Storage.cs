using FlowDance.Common.Events;
using FlowDance.Common.Commands;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Diagnostics;

namespace FlowDance.Client.RabbitMQUtils;

/// <summary>
///This class handles the reading and storing of messages to RabbitMQ. 
/// 
/// Based on code from this site - https://rabbitmq.github.io/rabbitmq-stream-dotnet-client/stable/htmlsingle/index.html
/// </summary>
public class Storage
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Producer> _producerLogger;
    private readonly ILogger<Consumer> _consumerLogger;
    private readonly ILogger<StreamSystem> _streamLogger;

    public Storage(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;

        _producerLogger = _loggerFactory.CreateLogger<Producer>();
        _consumerLogger = _loggerFactory.CreateLogger<Consumer>();
        _streamLogger = _loggerFactory.CreateLogger<StreamSystem>();
    }

    public void StoreEvent(Span span)
    {
        var streamName = span.TraceId.ToString();
        var confirmationTaskCompletionSource = new TaskCompletionSource<int>();

        //Check if stream/queue exist. 
        if (StreamExistOrQueue(streamName))
        {
            // Only first span in stream should be a root span.
            if (span is SpanOpened)
                ((SpanOpened)span).IsRootSpan = false;

            // Get StreamSystem
            var streamSystem = SingletonStreamSystem.GetInstance(_streamLogger).GetStreamSystem();

            // Validate against previous events grouped by the same TraceId. 
            ValidateStoredSpans(ReadAllSpansFromStream(span.TraceId.ToString()));

            // Create producer
            var producer = CreateProducer(streamName, streamSystem, confirmationTaskCompletionSource, _producerLogger);

            // Send a messages     
            var message = new Message(Encoding.Default.GetBytes(JsonConvert.SerializeObject(span, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));
            producer.Send(message);

            // Wait for confirmation feedback 
            confirmationTaskCompletionSource.Task.Wait();

            // Close producer
            producer.Close();
        }
        else // Stream don´t exists.
        {
            // SpanClosed should newer create the CreateQueue. Only SpanOpened are allowed to do that!  
            if (span is SpanClosed)
                throw new Exception("The event SpanClosed are trying to create a stream for the first time. This not allowed, only SpanOpened are allowed to do that!");

            if (span is SpanOpened)
                ((SpanOpened)span).IsRootSpan = true;

            // Create stream/queue
            CreateStream(streamName);

            // Get StreamSystem
            var streamSystem = SingletonStreamSystem.GetInstance(_streamLogger).GetStreamSystem();

            // Create producer
            var producer = CreateProducer(streamName, streamSystem, confirmationTaskCompletionSource, _producerLogger);

            // Send a messages     
            var message = new Message(Encoding.Default.GetBytes(JsonConvert.SerializeObject(span, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })));
            producer.Send(message);

            // Wait for confirmation feedback 
            confirmationTaskCompletionSource.Task.Wait();

            // Close producer
            producer.Close();
        }
    }

    public void StoreCommand(DetermineCompensation command)
    {
        var channel = SingletonConnection.GetInstance().GetConnection().CreateModel();
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

    public List<Span> ReadAllSpansFromStream(string streamName)
    {
        return ReadAllSpansFromStream(streamName, _consumerLogger);
    }

    private void ValidateStoredSpans(List<Span> spanList)
    {
        
        if (spanList.Any())
        {
            var sw = new Stopwatch();
            sw.Start();

            // Rule #1 - Can´t add Span after the root Span has been closed.
            var spanOpened = spanList[0];
            var spanClosed = from s in spanList
                             where s.SpanId == spanOpened.SpanId && s.GetType() == typeof(SpanClosed)
                             select s;

            if (spanClosed.Any())
                throw new Exception("Spans can´t be add after the root Span has been closed");

            sw.Stop();
            _streamLogger.LogInformation("A call to ValidateStoredSpans runs for {0} ms.", sw.Elapsed.TotalMilliseconds);
        }

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
    private List<Span> ReadAllSpansFromStream(string streamName, ILogger<Consumer> consumerLogger)
    {
        var sw = new Stopwatch();
        sw.Start();

        var numberOfMessages = GetLastOffset(streamName, consumerLogger) + 1;
        var spanList = new List<Span>();

        if (numberOfMessages > 0)
        {
            var consumerTaskCompletionSource = new TaskCompletionSource<int>();
            var streamSystem = SingletonStreamSystem.GetInstance(_streamLogger).GetStreamSystem();
            int numberOfMessageReceived = 0;

            var consumer = Consumer.Create(
                    new ConsumerConfig(streamSystem, streamName)
                    {
                        OffsetSpec = new OffsetTypeFirst(),
                        ClientProvidedName = "FlowDance.Client.Consumer",
                        MessageHandler = async (stream, consumer, context, message) =>
                        {
                            try
                            {
                                var messageContent = JsonConvert.DeserializeObject<Span>(Encoding.UTF8.GetString(message.Data.Contents), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
                                if (messageContent != null)
                                    spanList.Add(messageContent);

                                numberOfMessageReceived++;

                                if (numberOfMessageReceived == (int)numberOfMessages)
                                    consumerTaskCompletionSource.SetResult(1);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("", ex);
                            }

                            await Task.CompletedTask;
                        }
                    }, consumerLogger).GetAwaiter().GetResult();

            consumerTaskCompletionSource.Task.Wait();

            consumer.Close();

            sw.Stop();
            _streamLogger.LogInformation("A call to ReadAllSpansFromStream runs for {0} ms.", sw.Elapsed.TotalMilliseconds);
        }

        return spanList;
    }

    /// <summary>
    /// Check if a queue/stream exists. 
    /// </summary>
    /// <param name="name"></param>
    /// <returns>True if stream exists, else false.</returns>
    /// <exception cref="Exception"></exception>
    public bool StreamExistOrQueue(string name)
    {
        try
        {
            var sw = new Stopwatch();
            sw.Start();

            var channel = SingletonConnection.GetInstance().GetConnection().CreateModel();
            QueueDeclareOk ok = channel.QueueDeclarePassive(name);
            channel.Close();

            sw.Stop();
            _streamLogger.LogInformation("A call to StreamExistOrQueue runs for {0} ms.", sw.Elapsed.TotalMilliseconds);
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
    public void CreateStream(string streamName)
    {
        var sw = new Stopwatch();
        sw.Start();

        var streamSystem = SingletonStreamSystem.GetInstance(_streamLogger).GetStreamSystem();
        streamSystem.CreateStream(
            new StreamSpec(streamName) { });

        sw.Stop();
        _streamLogger.LogInformation("A call to StreamExistOrQueue runs for {0} ms.", sw.Elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Delete a stream. 
    /// </summary>
    /// <param name="streamName"></param>
    public void DeleteStream(string streamName)
    {
        var streamSystem = SingletonStreamSystem.GetInstance(_streamLogger).GetStreamSystem();
        streamSystem.DeleteStream(streamName);
    }

    /// <summary>
    /// Creates a producer. See https://github.com/rabbitmq/rabbitmq-stream-dotnet-client/blob/main/docs/Documentation/ProducerUsage.cs
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="streamSystem"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private Producer CreateProducer(string streamName, StreamSystem streamSystem, TaskCompletionSource<int> confirmationTaskCompletionSource, ILogger<Producer> procuderLogger)
    {
        var sw = new Stopwatch();
        sw.Start();

        var producer = Producer.Create(
            new ProducerConfig(
                streamSystem,
                streamName)
            {
                ClientProvidedName = "FlowDance.Client.Producer",
                ConfirmationHandler = async confirmation =>
                {
                    switch (confirmation.Status)
                    {
                        case ConfirmationStatus.Confirmed:
                            Console.WriteLine("Message confirmed");
                            break;
                        case ConfirmationStatus.ClientTimeoutError:
                        case ConfirmationStatus.StreamNotAvailable:
                        case ConfirmationStatus.InternalError:
                        case ConfirmationStatus.AccessRefused:
                        case ConfirmationStatus.PreconditionFailed:
                        case ConfirmationStatus.PublisherDoesNotExist:
                        case ConfirmationStatus.UndefinedError:
                            Console.WriteLine("Message not confirmed with error: {0}", confirmation.Status);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    confirmationTaskCompletionSource.SetResult(1);

                    await Task.CompletedTask;
                }
            }, procuderLogger).GetAwaiter().GetResult();

        sw.Stop();
        _streamLogger.LogInformation("A call to CreateProducer runs for {0} ms.", sw.Elapsed.TotalMilliseconds);

        return producer;
    }
}

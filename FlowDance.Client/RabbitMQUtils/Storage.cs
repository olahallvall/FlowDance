using FlowDance.Common.Events;
using FlowDance.Common.Commands;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace FlowDance.Client.RabbitMQUtils;

/// <summary>
/// https://rabbitmq.github.io/rabbitmq-stream-dotnet-client/stable/htmlsingle/index.html
/// </summary>
public class Storage
{
    private ILoggerFactory _loggerFactory;
    private ILogger<Producer> _producerLogger;
    private ILogger<Consumer> _consumerLogger;
    private ILogger<StreamSystem> _streamLogger;

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
        if (StreamExist(streamName))
        {
            // Only first span in stream should be a root span.
            if (span is SpanOpened)
                ((SpanOpened)span).IsRootSpan = false;

            // Get StreamSystem
            var streamSystem = SingletonStreamSystem.getInstance(_streamLogger).getStreamSystem();

            // Validate against previous events grouped by the same TraceId. 
            ValidateStoredSpans(ReadAllSpansFromStream(span.TraceId.ToString()));

            // Create producer
            Producer producer = CreateProducer(streamName, streamSystem, confirmationTaskCompletionSource, _producerLogger);

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
            var streamSystem = SingletonStreamSystem.getInstance(_streamLogger).getStreamSystem();

            // Create producer
            Producer producer = CreateProducer(streamName, streamSystem, confirmationTaskCompletionSource, _producerLogger);

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

    }

    public List<Span> ReadAllSpansFromStream(string streamName)
    {
        return ReadAllSpansFromStream(streamName, _consumerLogger);
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
    /// Get the Offset-number from the last massage in the stream.
    /// Use this method very carefully!!! The stream needs to have at least one message. If not this method will wait unti one message arrives.
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="consumerLogger"></param>
    /// <returns></returns>
    private ulong GetLastOffset(string streamName, ILogger<Consumer> consumerLogger)
    {
        var consumerTaskCompletionSource = new TaskCompletionSource<int>();
        var streamSystem = SingletonStreamSystem.getInstance(_streamLogger).getStreamSystem();
        ulong lastOffset = 0;

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
                }, consumerLogger).Result;

        consumerTaskCompletionSource.Task.Wait();

        consumer.Close();

        return lastOffset;
    }

    /// <summary>
    /// Reads all messages from the stream.
    /// Use this method very carefully!!! The stream needs to have at least one message. If not this method will wait unti one message arrives.
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="consumerLogger"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private List<Span> ReadAllSpansFromStream(string streamName, ILogger<Consumer> consumerLogger)
    {
        var numberOfMessages = GetLastOffset(streamName, consumerLogger) + 1;
        var spanList = new List<Span>();

        if (numberOfMessages > 0)
        {
            var consumerTaskCompletionSource = new TaskCompletionSource<int>();
            var streamSystem = SingletonStreamSystem.getInstance(_streamLogger).getStreamSystem();
            int numberOfMessageRecived = 0;

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
                                
                                numberOfMessageRecived++;

                                if (numberOfMessageRecived == (int)numberOfMessages)
                                    consumerTaskCompletionSource.SetResult(1);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("", ex);
                            }

                            await Task.CompletedTask;
                        }
                    }, consumerLogger).Result;

            consumerTaskCompletionSource.Task.Wait();

            consumer.Close();
        }

        return spanList;
    }

    /// <summary>
    /// Check if a queue/stream exists. 
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="logger"></param>
    /// <returns>True if queue/stream exists, else false.</returns>
    /// <exception cref="Exception"></exception>
    public bool StreamExist(string streamName) 
    {  
        try {
            var channel = SingletonConnection.getInstance().getConnection().CreateModel();
            QueueDeclareOk ok = channel.QueueDeclarePassive(streamName);
            channel.Close();
        } 
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex) 
        {
            if(ex.Message.Contains("no queue"))
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
        var streamSystem = SingletonStreamSystem.getInstance(_streamLogger).getStreamSystem();
        streamSystem.CreateStream(
            new StreamSpec(streamName) { } );
    }

    /// <summary>
    /// Delete a stream. 
    /// </summary>
    /// <param name="streamName"></param>
    public void DeleteStream(string streamName)
    {        
        var streamSystem = SingletonStreamSystem.getInstance(_streamLogger).getStreamSystem();
        streamSystem.DeleteStream(streamName);        
    }

    /// <summary>
    /// Creates a producer. See https://github.com/rabbitmq/rabbitmq-stream-dotnet-client/blob/main/docs/Documentation/ProducerUsage.cs
    /// </summary>
    /// <param name="StreamName"></param>
    /// <param name="streamSystem"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private Producer CreateProducer(string StreamName, StreamSystem streamSystem, TaskCompletionSource<int> confirmationTaskCompletionSource, ILogger<Producer> procuderLogger)
    {
        var producer = Producer.Create(
            new ProducerConfig(
                streamSystem,
                StreamName)
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
            }, procuderLogger).Result;

        return producer;
    }
}

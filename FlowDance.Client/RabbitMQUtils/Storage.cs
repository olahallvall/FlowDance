using FlowDance.Common.Events;
using FlowDance.Common.Commands;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using System;

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
        if (StreamExist(streamName, _producerLogger))
        {
            // Only first span in stream should be a root span.
            if (span is SpanOpened)
                ((SpanOpened)span).IsRootSpan = false;
        }
        else
        {
            // SpanClosed should newer create the CreateQueue. Only SpanOpened are allowed to do that!  
            if (span is SpanClosed)
                throw new Exception("The event SpanClosed are trying to create a stream for the first time. This not allowed, only SpanOpened are allowed to do that!");

            if (span is SpanOpened)
                ((SpanOpened)span).IsRootSpan = true;

            // Create stream/queue
            CreateStream(streamName, _producerLogger);
        }

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

    public void StoreCommand(DetermineCompensation command)
    {

    }

    public List<Span> ReadAllSpansFromStream(string streamName)
    {
        return ReadAllSpansFromStream(streamName, _consumerLogger);
    }

    private List<Span> ReadAllSpansFromStream(string streamName, ILogger<Consumer> consumerLogger)
    {
        var consumerTaskCompletionSource = new TaskCompletionSource<int>();
        var streamSystem = SingletonStreamSystem.getInstance(_streamLogger).getStreamSystem();
        var spanList = new List<Span>();
        var type = Type.GetType("Span");

        var consumer = Consumer.Create(
                new ConsumerConfig(streamSystem, streamName)
                {
                    OffsetSpec = new OffsetTypeFirst(),
                    MessageHandler = async (stream, consumer, context, message) => 
                    {
                        try
                        {
                            var messageContent = JsonConvert.DeserializeObject<Span>(Encoding.UTF8.GetString(message.Data.Contents), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
                            if(messageContent != null ) 
                                spanList.Add(messageContent);
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
        return spanList;
    }

    /// <summary>
    /// Check if a queue/stream exists. 
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="logger"></param>
    /// <returns>True if queue/stream exists, else false.</returns>
    /// <exception cref="Exception"></exception>
    private bool StreamExist(string streamName, ILogger logger) 
    {  
        try {
            var channel = SingletonConnection.getInstance().getConnection().CreateModel();
            channel.QueueDeclarePassive(streamName);
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
    /// <param name="logger"></param>
    private void CreateStream(string streamName, ILogger logger)
    {
        var channel = SingletonConnection.getInstance().getConnection().CreateModel();
        Dictionary<string, object> arguments = new Dictionary<string, object> { { "x-queue-type", "stream" } };
        channel.QueueDeclare(streamName, true, false, false, arguments);
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

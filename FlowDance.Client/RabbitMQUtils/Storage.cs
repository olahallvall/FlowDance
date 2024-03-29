using FlowDance.Common.Events;
using FlowDance.Common.Commands;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;

namespace FlowDance.Client.RabbitMQUtils;

/// <summary>
/// https://rabbitmq.github.io/rabbitmq-stream-dotnet-client/stable/htmlsingle/index.html
/// </summary>
public class Storage
{
    private ILoggerFactory _loggerFactory;
    private ILogger<Producer> _producerLogger;
    private ILogger<StreamSystem> _streamLogger;

    public Storage(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;

        _producerLogger = _loggerFactory.CreateLogger<Producer>();
        _streamLogger = _loggerFactory.CreateLogger<StreamSystem>();
    }

    public void StoreEvent(Span span)
    {
        var streamName = span.TraceId.ToString();


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
                throw new Exception("The event SpanClosed are trying to create a queue. This not allowed, only SpanOpened are allowed to do that!");

            if (span is SpanOpened)
                ((SpanOpened)span).IsRootSpan = true;

            // Create stream/queue
            CreateStream(streamName, _producerLogger);
        }

        // Create StreamSystem
        var streamSystem = SingletonStreamSystem.getInstance(_streamLogger).getStreamSystem();

        // Create producer
        Producer producer = CreateProducer(streamName, streamSystem, _producerLogger);

        // Send a messages
        var message = new Message(Encoding.Default.GetBytes(JsonConvert.SerializeObject(span))); 
        producer.Send(message).ConfigureAwait(true); 

        producer.Close().ConfigureAwait(true); 
    }

    public void StoreCommand(DetermineCompensation command)
    {

    }

    private bool StreamExist(string streamName, ILogger logger) 
    {  
        try {
            using var channel = SingletonConnection.getInstance().getConnection().CreateModel();

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

    private void CreateStream(string streamName, ILogger logger)
    {
        using var channel = SingletonConnection.getInstance().getConnection().CreateModel();
        Dictionary<string, object> arguments = new Dictionary<string, object> { { "x-queue-type", "stream" } };

        channel.QueueDeclare(streamName, true, false, false, arguments);
    }

    /// <summary>
    ///  https://github.com/rabbitmq/rabbitmq-stream-dotnet-client/blob/main/docs/Documentation/ProducerUsage.cs
    /// </summary>
    /// <param name="StreamName"></param>
    /// <param name="streamSystem"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private Producer CreateProducer(string StreamName, StreamSystem streamSystem, ILogger<Producer> logger)
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
                    await Task.CompletedTask.ConfigureAwait(false);
                }
            }, logger).Result;

        return producer;
    }
}

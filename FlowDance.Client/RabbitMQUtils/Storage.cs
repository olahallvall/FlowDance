using FlowDance.Common.Events;
using FlowDance.Common.Commands;
using RabbitMQ.Stream.Client; 
using RabbitMQ.Stream.Client.Reliable;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace FlowDance.Client.RabbitMQUtils;

public class Storage
{
    public void StoreEvent(Span span)
    {
        var streamName = span.TraceId.ToString();

        //Check if stream/queue exist. 
        if (StreamExist(streamName))
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
            CreateStream(streamName);
        }

        var streamSystem = StreamSystem.Create(new StreamSystemConfig() 
        {
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            Endpoints = new List<EndPoint>() { new IPEndPoint(IPAddress.Loopback, 5552) }
        }, null).Result;

        var confirmationTaskCompletionSource = new TaskCompletionSource<int>();
        var producer = CreateProducer(streamName, streamSystem, confirmationTaskCompletionSource);

        // Send a messages
        producer.Send(new Message(Encoding.ASCII.GetBytes($"A"))).ConfigureAwait(false);

        confirmationTaskCompletionSource.Task.Wait();
        producer.Close().ConfigureAwait(false); 
    }

    public void StoreCommand(DetermineCompensation command)
    {

    }

    private bool StreamExist(string streamName) 
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

    private void CreateStream(string streamName)
    {
        using var channel = SingletonConnection.getInstance().getConnection().CreateModel();
        Dictionary<string, object> arguments = new Dictionary<string, object> { { "x-queue-type", "stream" } };

        channel.QueueDeclare(streamName, true, false, false, arguments);
    }

    private Producer CreateProducer(string StreamName, StreamSystem streamSystem, TaskCompletionSource<int> confirmationTaskCompletionSource)
    {
        var confirmationCount = 0;
        const int MessageCount = 100;

        var producer = Producer.Create(new ProducerConfig(streamSystem, StreamName)
        {
            ConfirmationHandler = async confirmation => 
            {
                Interlocked.Increment(ref confirmationCount);

                // here you can handle the confirmation
                switch (confirmation.Status)
                {
                    case ConfirmationStatus.Confirmed: 
                        // all the messages received here are confirmed
                        if (confirmationCount == MessageCount)
                        {
                            Console.WriteLine("*********************************");
                            Console.WriteLine($"All the {MessageCount} messages are confirmed");
                            Console.WriteLine("*********************************");
                        }

                        break;

                    case ConfirmationStatus.StreamNotAvailable:
                    case ConfirmationStatus.InternalError:
                    case ConfirmationStatus.AccessRefused:
                    case ConfirmationStatus.PreconditionFailed:
                    case ConfirmationStatus.PublisherDoesNotExist:
                    case ConfirmationStatus.UndefinedError:
                    case ConfirmationStatus.ClientTimeoutError:
                        // (4)
                        Console.WriteLine(
                            $"Message {confirmation.PublishingId} failed with {confirmation.Status}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (confirmationCount == MessageCount)
                {
                    confirmationTaskCompletionSource.SetResult(MessageCount);
                }

                await Task.CompletedTask.ConfigureAwait(false);
            }
        }, null).Result;

        return producer;
    }

}

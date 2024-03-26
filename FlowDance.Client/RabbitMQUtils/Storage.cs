using FlowDance.Common.Events;
using FlowDance.Common.Commands;
using RabbitMQ.Client;
using System.Collections.Generic;
using System;
using System.Threading.Channels;


namespace FlowDance.Client.RabbitMQUtils;

public class Storage
{
    public void StoreEvent(Span span)
    {
        string QueueName = span.TraceId.ToString();

        //Check if stream/queue exist. 
        if (QueueExist(QueueName))
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
            CreateQueue(QueueName);
        }
    }

    public void StoreCommand(DetermineCompensation command)
    {

    }

    private bool QueueExist(string QueueName) 
    {  
        try {
            using var channel = SingletonConnection.getInstance().getConnection().CreateModel();

            channel.QueueDeclarePassive(QueueName);  
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

    private void CreateQueue(string QueueName)
    {
        using var channel = SingletonConnection.getInstance().getConnection().CreateModel();
        Dictionary<string, object> arguments = new Dictionary<string, object>
        {
            { "x-queue-type", "stream" }
        };

        channel.QueueDeclare(QueueName, true, false, false, arguments);
    }
}

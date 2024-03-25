using FlowDance.Common.Events;
using FlowDance.Common.Commands;
using RabbitMQ.Client;
using System;


namespace FlowDance.Client.RabbitMQUtils;

public class Storage
{
    public void StoreEvent(Span span)
    {
        if(span is SpanOpened)
        {
            //Check if stream/queue exist. Only first span i stream should be a root span.
            if (!QueueExist(span.TraceId.ToString())) 
                ((SpanOpened)span).IsRootSpan = true;
            else
                ((SpanOpened)span).IsRootSpan = false;
        }
    }

    public void StoreCommand(DetermineCompensation command)
    {

    }

    public bool QueueExist(string QueueName) 
    {  
        try {
            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclarePassive(QueueName);  
        } 
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex) 
        {
            return false; 
        }
        catch (Exception ex)
        {
            throw new Exception("Non suspected exception occured. See inner exception for more info", ex);
        }
        return true; 
    }
}

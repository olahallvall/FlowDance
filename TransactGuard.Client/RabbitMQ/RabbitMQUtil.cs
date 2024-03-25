using TransactGuard.Common.Events;
using TransactGuard.Common.Commands;
using RabbitMQ.Client;
using System;


namespace TransactGuard.Client.RabbitMQ;

public class RabbitMQUtil
{
    public void StoreEvent(Span span)
    {
        if(span is SpanOpened)
        {
            //Check if stream/queue exist. Only first span i stream should be a root span.
            if (QueueExist(span.TraceId.ToString())) 
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
        bool b = false; 
        try {
            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var xx = channel.QueueDeclarePassive(QueueName); 
            b = true; 
        } 
        catch (Exception x) 
        {
            Console.WriteLine("Queue {0} does not exist", x.Message); 
        }
        return b; 
    }
}

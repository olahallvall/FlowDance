﻿using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace FlowDance.AzureFunctions.Functions
{
    public class RabbitMqCompensating
    {
        public RabbitMqCompensating()
        {
        }

        [Function(nameof(RabbitMqCompensate))]
        public async Task<bool> RabbitMqCompensate([ActivityTrigger] string spanJson, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("RabbitMqCompensating");
            var span = JsonConvert.DeserializeObject<Span>(spanJson, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            if (span == null)
            {
                logger.LogError("There no Span data! The function RabbitMqCompensate has nothing to work with and will exit.");
                throw new HttpRequestException("There no Span data! The function RabbitMqCompensate has nothing to work with and will exit.", null);
            }
            
            var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
            var connectionFactory = new ConnectionFactory();
            config.GetSection("RabbitMqConnection").Bind(connectionFactory);

            var connection = connectionFactory.CreateConnection();
            var channel = connection.CreateModel();
            var compensatingAction = (AmqpCompensatingAction)span.SpanOpened.CompensatingAction;

            if (!span.CompensationData.Any())
                span.CompensationData.Add(new Common.Events.SpanCompensationData() { CompensationData = span.TraceId.ToString(), Identifier = "default" });

            IBasicProperties props = channel.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>();
            
            // Set headers
            // https://stackoverflow.com/questions/69877042/how-should-i-pass-a-string-value-in-a-rabbitmq-header
            if (compensatingAction.Headers != null)
            {
                foreach (var header in compensatingAction.Headers)
                {
                    props.Headers.Add(header.Key, header.Value);
                }

                // Always add a this headers
                if (!compensatingAction.Headers.ContainsKey("x-correlation-id"))
                    props.Headers.Add("x-correlation-id", span.TraceId.ToString());

                if (!compensatingAction.Headers.ContainsKey("x-calling-function-name"))
                    props.Headers.Add("x-calling-function-name", span.SpanOpened.CallingFunctionName);
            }
            else
            {
                props.Headers.Add("x-correlation-id", span.TraceId.ToString());
                props.Headers.Add("x-calling-function-name", span.SpanOpened.CallingFunctionName);
            }

            // So we can Confirm
            channel.ConfirmSelect();

            // Store the messages
            channel.BasicPublish(exchange: string.Empty,
                    routingKey: compensatingAction.QueueName,
                    basicProperties: props,
                    body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(span.CompensationData)));

            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
         
            return true;
        }
    }
}

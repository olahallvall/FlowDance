using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace FlowDance.Server.Functions
{
    public class RabbitMqCompensating
    {
        private readonly IConfiguration _configuration;
        private readonly CreateChannelOptions _channelOpts;
        private const ushort MAX_OUTSTANDING_CONFIRMS = 256;
        public RabbitMqCompensating(IConfiguration configuration)
        {
            _configuration = configuration;

            _channelOpts = new CreateChannelOptions(
                   publisherConfirmationsEnabled: true,
                   publisherConfirmationTrackingEnabled: true,
                   outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(MAX_OUTSTANDING_CONFIRMS)
            );
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

            var connectionFactory = new ConnectionFactory();
            connectionFactory.Uri = new Uri(_configuration["RabbitMq_Connection"]);

            var connection = await connectionFactory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync(_channelOpts);
            var compensatingAction = (AmqpCompensatingAction)span.SpanOpened.CompensatingAction;

            if (!span.CompensationData.Any())
                span.CompensationData.Add(new Common.Events.SpanCompensationData() { CompensationData = span.TraceId.ToString(), Identifier = "default" });

            var props = new BasicProperties { Persistent = true };
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

            // Store the messages
            await channel.BasicPublishAsync(exchange: string.Empty,
                    routingKey: compensatingAction.QueueName,
                    mandatory: true,
                    basicProperties: props,
                    body: Encoding.Default.GetBytes(JsonConvert.SerializeObject(span.CompensationData)));
            
            return true;
        }
    }
}

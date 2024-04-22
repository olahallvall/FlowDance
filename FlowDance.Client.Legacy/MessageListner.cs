using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowDance.Client.Legacy
{
    public class MessageListener :  IDisposable
    {
        //private readonly IOptions<RMQConfig> _rabbitOptions;
        private readonly ILogger<MessageListener> _logger;
        private Timer _timer;
        private int executionCount = 0;
        private readonly IConnection _connection;

        public MessageListener(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MessageListener>();
            //_rabbitOptions = rabbitOptions;
            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };
            _connection = factory.CreateConnection();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;

        }

        private void DoWork(object state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation("MessageListener is working. Count: {Count}", count);

            var channel = _connection.CreateModel();

            channel.BasicQos(0, 100, false);
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body.ToArray());
                _logger.LogInformation($"consume {message}");
            };

            channel.BasicConsume(queue: "c8d8070d-7680-4a70-83f1-910672af9c76", autoAck: false, consumer: consumer, arguments: new Dictionary<string, object> { { "x-stream-offset", 0 } });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MessageListener is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            //Close _connection
            _timer?.Dispose();
        }
    }
}

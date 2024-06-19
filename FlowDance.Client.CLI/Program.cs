using Microsoft.Extensions.Logging;
using FlowDance.Client.CLI.RabbitMq;
using Newtonsoft.Json;

namespace FlowDance.Client.CLI;

internal class Program
{
    static void Main(string[] args)
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        var storage = new Storage(loggerFactory);
        var spanEvents = storage.ReadAllSpanEventsFromStream("d4e2ddd9-6a71-410d-b985-023a8412ebda");

        string output = JsonConvert.SerializeObject(spanEvents, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });

        Console.WriteLine(output);
    }
}
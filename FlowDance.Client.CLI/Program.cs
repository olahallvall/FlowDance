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
        var spanEvents = storage.ReadAllSpanEventsFromStream("b105028e-db96-4a57-a8f9-b47586055e7b");

        string output = JsonConvert.SerializeObject(spanEvents, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });

        Console.WriteLine(output);
    }
}
using Microsoft.Extensions.Logging;
using FlowDance.Client.CLI.StorageProviders;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using CommandLine;

namespace FlowDance.Client.CLI;

internal class Program
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }

    static void Main(string[] args)
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        var storage = new Storage(loggerFactory);
        var spanEvents = storage.ReadAllSpanEventsFromStream("08d29b86-513e-4a6c-8487-b01e393cddee");

        string output = JsonConvert.SerializeObject(spanEvents, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });
        Console.WriteLine(output);

        Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.Verbose)
                       {
                           Console.WriteLine($"Verbose output enabled. Current Arguments: -v {o.Verbose}");
                           Console.WriteLine("Quick Start Example! App is in Verbose mode!");
                       }
                       else
                       {
                           Console.WriteLine($"Current Arguments: -v {o.Verbose}");
                           Console.WriteLine("Quick Start Example!");
                       }
                   });

    }
}
﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using FlowDance.Client;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddFilter("RabbitMQ.Stream", LogLevel.Information);
            });

            var config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
            string HotelServiceCompensationUrl = config.GetSection("MySettings").GetSection("CompensationUrls").GetSection("HotelService").Value;

            using (CompensationScope compScope = new CompensationScope(HotelServiceCompensationUrl, Guid.NewGuid(), factory)) 
            {

                // Boka taxi


                // Boka flyg


                compScope.Commit();
            } 
        }
    }
}
﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FlowDance.Client.RabbitMQUtils;

namespace FlowDance.Tests;

[TestClass]
public class StorageTests
{
    private static ILoggerFactory _factory = null!;
    private static IConfigurationRoot _config = null!;

    [ClassInitialize()]
    public static void ClassInit(TestContext context)
    {
        _factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddFilter("RabbitMQ.Stream", LogLevel.Information);
        });

        _config = new ConfigurationBuilder().AddJsonFile($"appsettings.json").Build();
    }

    [TestMethod]
    public void ReadAllSpansFromStream()
    {
        var storage = new Storage(_factory);
        var spanList = storage.ReadAllSpansFromStream("70c9c2b9-ef4f-4f22-8e3f-a7e8254a54d0");

        spanList.Count();



    }
}
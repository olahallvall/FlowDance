using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FlowDance.Server.Triggers.Http
{
    public class ManagementHttpFunction
    {
        private readonly ILogger<ManagementHttpFunction> _logger;

        public ManagementHttpFunction(ILogger<ManagementHttpFunction> logger)
        {
            _logger = logger;
        }

        [Function("ManagementHttpFunction")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}

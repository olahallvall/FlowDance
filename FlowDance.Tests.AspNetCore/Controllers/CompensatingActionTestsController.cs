using Microsoft.AspNetCore.Mvc;
using FlowDance.Client;
using FlowDance.Common.Enums;
using FlowDance.Client.AspNetCore.ActionFilters;

namespace FlowDance.Tests.AspNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompensatingActionTestsController : ControllerBase
    {
   
        private readonly ILogger<CompensatingActionTestsController> _logger;

        public CompensatingActionTestsController(ILogger<CompensatingActionTestsController> logger)
        {
            _logger = logger;
        }

        [CompensationSpan(CompensationSpanOption = CompensationSpanOption.RequiresNewBlockingCallChain)]
        [Route("SaveWithoutCompensatingActionUrl")]
        [HttpPost]
        public void SaveWithoutCompensatingActionUrl()
        {
            // Access the CompensationSpan instance from the ActionFilter
            var compensationSpan = HttpContext.Items["CompensationSpan"] as CompensationSpan;

            compensationSpan.AddCompensationData("fffff");

            var traceId = compensationSpan.TraceId;
        }

        [CompensationSpan(CompensatingActionUrl = "http://localhost/TripBookingService/Compensation", CompensationSpanOption = CompensationSpanOption.RequiresNewBlockingCallChain)]
        [Route("SaveAsCompensatingActionUrl")]
        [HttpPost]
        public void SaveAsCompensatingActionUrl()
        {
            // Access the CompensationSpan instance from the ActionFilter
            var compensationSpan = HttpContext.Items["CompensationSpan"] as CompensationSpan;

            compensationSpan.AddCompensationData("fffff");

            var traceId = compensationSpan.TraceId;
        }

        [CompensationSpan(CompensatingActionQueueName = "FlowDance.Tests.AspNetCore.CompensatingCommands", CompensationSpanOption = CompensationSpanOption.RequiresNewBlockingCallChain)]
        [Route("CompensatingActionQueueName")]
        [HttpPost]
        public void CompensatingActionQueueName()
        {
            // Access the CompensationSpan instance from the ActionFilter
            var compensationSpan = HttpContext.Items["CompensationSpan"] as CompensationSpan;

            compensationSpan.AddCompensationData("fffff");

            var traceId = compensationSpan.TraceId;
        }
    }
}

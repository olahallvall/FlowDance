using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace FlowDance.Client.AspNetCore.ActionFilters
{
    public class CompensationSpanAttribute : ActionFilterAttribute
    {
        public required string CompensatingActionUrl { get; set; }
        public required CompensationSpanOption CompensationSpanOption { get; set; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //if (!context.ActionDescriptor.IsControllerAction())
            //{
            //    await next();
            //    return;
            //}

            var callingFunctionName = string.Empty;
            if (context?.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                callingFunctionName = descriptor.ControllerName + "." + descriptor.ActionName;
            }

            var serviceProvider = context.HttpContext.RequestServices;
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            ILogger<CompensationSpanAttribute> logger = loggerFactory.CreateLogger<CompensationSpanAttribute>();


            // Get x-correlation-id
            Guid traceId;
            if (CompensationSpanOption == CompensationSpanOption.RequiresNew)
                traceId = Guid.NewGuid();
            else
            {
                var b1 = context.HttpContext.Request.Headers.TryGetValue("x-correlation-id", out var correlationId);
                var b2 = Guid.TryParse(correlationId, out traceId);
            }

            ICompensationSpan compensationSpan = null;
            if (CompensatingActionUrl.Contains("http"))
                compensationSpan = new CompensationSpan(new HttpCompensatingAction(CompensatingActionUrl), traceId, loggerFactory, callingFunctionName);
            else if(CompensatingActionUrl.Contains("amqp"))
                compensationSpan = new CompensationSpan(new AmqpCompensatingAction(CompensatingActionUrl), traceId, loggerFactory, callingFunctionName);

            if(compensationSpan == null)
                throw new Exception("Can't create a CompensationSpan.");
            
            // Make the CompensationSpan avalible to the Controller.
            // Access this from the controller method using this code; var compensationSpan = HttpContext.Items["CompensationSpan"];
            var controller = (ControllerBase)context.Controller;
            controller.HttpContext.Items.Add("CompensationSpan", compensationSpan);

            var result = await next();

            // Funkar detta om flera ActionFilterAttribute exekverar efter varandra???
            if (result.Exception == null || result.ExceptionHandled)
            {
                compensationSpan.Complete();
            }

            compensationSpan.Dispose();
        }
    }
}

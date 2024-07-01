using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using FlowDance.Common.Enums;

namespace FlowDance.Client.AspNetCore.ActionFilters
{
    /// <summary>
    /// The CompensationSpan action filter provides a simple way to add a controller method participating in a flow dance/transaction that can be compensated.
    /// The Complete method will be automatically called if the controller does not throw an exception.
    /// 
    /// To access a <CompensationSpan> instance inside a controller method, you can use this code; var compensationSpan = HttpContext.Items["CompensationSpan"] as CompensationSpan; 
    /// </summary>
    public class CompensationSpanAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Use a string to point out url/amqp end point to use when compensating. 
        /// <example>
        /// <code>
        /// [CompensationSpan(CompensatingActionUrl = "http://localhost/TripBookingService/Compensation", CompensationSpanOption = CompensationSpanOption.RequiresNewBlockingCalls)]
        /// </code>
        /// </example>
        /// </summary>
        public required string CompensatingActionUrl { get; set; }

        public CompensationSpanOption CompensationSpanOption { get; set; }

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
            var logger = loggerFactory.CreateLogger<CompensationSpanAttribute>();

            // Get traceId
            Guid traceId = Guid.NewGuid(); 
            if (CompensationSpanOption == CompensationSpanOption.Required)
            {
                context.HttpContext.Request.Headers.TryGetValue("x-correlation-id", out var correlationId);
                var isValid = Guid.TryParse(correlationId, out traceId);
                if (!isValid)
                    throw new Exception("CorrelationId/TraceId (" + correlationId + ") are not a valid Guid.");
            }

            ICompensationSpan compensationSpan = null;
            if (CompensatingActionUrl.Contains("http"))
                compensationSpan = new CompensationSpan(new HttpCompensatingAction(CompensatingActionUrl), traceId, loggerFactory, CompensationSpanOption, callingFunctionName);
            else if (CompensatingActionUrl.Contains("amqp"))
                compensationSpan = new CompensationSpan(new AmqpCompensatingAction(CompensatingActionUrl), traceId, loggerFactory, CompensationSpanOption, callingFunctionName);

            if (compensationSpan == null)
                throw new Exception("Can't create a CompensationSpan.");

            // Make the CompensationSpan avalible to the Controller.
            // Access this from the controller method using this code; var compensationSpan = HttpContext.Items["CompensationSpan"] as CompensationSpan;
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

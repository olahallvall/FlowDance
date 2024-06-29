using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace FlowDance.AzureFunctions.Functions
{
    public class HttpCompensating
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpCompensating(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [Function(nameof(HttpCompensate))]
        public async Task<bool> HttpCompensate([ActivityTrigger] string spanJson, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("HttpCompensate");
            var span = JsonConvert.DeserializeObject<Span>(spanJson, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            if (span == null)
            {
                logger.LogError("There no Span data! The function HttpCompensate has noting to work with and will exit.");
                throw new Exception("There no Span data! The function HttpCompensate has nothing to work with and will end.");
            }
            var httpClient = _httpClientFactory.CreateClient();

            var compensatingAction = (HttpCompensatingAction)span.SpanOpened.CompensatingAction;
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, compensatingAction.Url);

            if (!span.CompensationData.Any())
                span.CompensationData.Add(new Common.Events.SpanCompensationData() { CompensationData = span.TraceId.ToString(), Identifier = "default" } );

            // Set content/body
            httpRequest.Content = new StringContent(JsonConvert.SerializeObject(span.CompensationData), Encoding.UTF8, $"application/json");

            // Set headers
            if (compensatingAction.Headers != null)
            {
                foreach (var header in compensatingAction.Headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }

                // Always add a this headers
                if (!compensatingAction.Headers.ContainsKey("x-correlation-id"))
                    httpRequest.Headers.Add("x-correlation-id", span.TraceId.ToString());

                if (!compensatingAction.Headers.ContainsKey("x-calling-function-name"))
                    httpRequest.Headers.Add("x-calling-function-name", span.SpanOpened.CallingFunctionName);
            }
            else
            {
                httpRequest.Headers.Add("x-correlation-id", span.TraceId.ToString());
                httpRequest.Headers.Add("x-calling-function-name", span.SpanOpened.CallingFunctionName);
            }

            var response = await httpClient.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {  
                // These StatusCode will not be retried.
                if(response.StatusCode == System.Net.HttpStatusCode.Forbidden || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogInformation("A HTTP POST to {url} returns {statuscode}. No retry will be performed for http codes 403 and 404", compensatingAction.Url, (int)response.StatusCode);
                    return false;
                }
                    
                logger.LogError("A HTTP POST to {url} returns {statuscode}. Retrying if client code configured for retry (RetryPolicy).", compensatingAction.Url, response.StatusCode);
                throw new Exception(string.Format("A HTTP POST to {url} returns {statuscode}.", compensatingAction.Url, response.StatusCode));
            }
         
            return true;
        }
    }
}

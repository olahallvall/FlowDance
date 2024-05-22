using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace FlowDance.AzureFunctions.Functions
{
    public class HttpCompensating
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpCompensating(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Function(nameof(HttpCompensate))]
        public async Task<string> HttpCompensate([ActivityTrigger] string spanJson, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("HttpCompensate");
            var span = JsonConvert.DeserializeObject<Span>(spanJson, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            if (span == null)
            {
                logger.LogError("There no Span data! The function HttpCompensate has noting to work with and will exit.");
                throw new HttpRequestException("There no Span data! The function HttpCompensate has noting to work with and will exit.", null);
            }
            var httpClient = _httpClientFactory.CreateClient();

            var compensatingAction = (HttpCompensatingAction)span.SpanOpened.CompensatingAction;
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, compensatingAction.Url);

            if (compensatingAction.PostData == null)
                compensatingAction.PostData = JsonConvert.SerializeObject(span.TraceId.ToString());

            // Set content/body
            httpRequest.Content = new StringContent(compensatingAction.PostData, Encoding.UTF8, $"application/json");

            // Set headers
            if (compensatingAction.Headers != null)
            {
                foreach (var header in compensatingAction.Headers)
                {
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue(header.Key, header.Value);
                }

                // Always add a this headers
                if (!compensatingAction.Headers.ContainsKey("x-correlation-id"))
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("x-correlation-id", span.TraceId.ToString());

                if (!compensatingAction.Headers.ContainsKey("calling-function-name"))
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("calling-function-name", span.SpanOpened.CallingFunctionName);

            }
            else
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("x-correlation-id", span.TraceId.ToString());
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("calling-function-name", span.SpanOpened.CallingFunctionName);
            }

            // Send HTTP POST
            try
            {
                var response = await httpClient.SendAsync(httpRequest).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    //throw new HttpRequestException(("A HTTP POST to {url} returns {statuscode}", compensatingAction.Url, response.StatusCode), null);
                    throw new HttpRequestException("A HTTP POST to {url} returns {statuscode}", null);
                }
            }
            // Filter by InnerException.
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                // Handle timeout.
                Console.WriteLine("Timed out: " + ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                // Handle cancellation.
                Console.WriteLine("Canceled: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Handles all exceptions.
                Console.WriteLine(ex.Message);
                throw;
            }

            return JsonConvert.SerializeObject(span, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });
        }
    }
}

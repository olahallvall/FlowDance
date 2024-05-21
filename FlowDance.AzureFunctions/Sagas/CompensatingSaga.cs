using FlowDance.Common.CompensatingActions;
using FlowDance.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace FlowDance.AzureFunctions.Sagas
{
    /// <summary>
    /// Generic Saga for compensate.  
    // See https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-code-constraints?tabs=csharp
    // https://www.tpeczek.com/2021/09/handling-transient-errors-in-durable.html
    /// </summary>
    public class CompensatingSaga
    {
        //private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public CompensatingSaga(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Function(nameof(CompensatingSaga))]
        public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger(nameof(CompensatingSaga));
            var json = context.GetInput<string>();

            var spanList = JsonConvert.DeserializeObject<List<Span>>(json, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            if (spanList == null || spanList.Count == 0)
            {
                logger.LogWarning("There no Spans in the SpanList! The Orchestrator has noting to work with and will exit.");
                return;
            }

            logger.LogInformation("Start CompensatingSaga for traceId {traceId}", spanList.First().TraceId);

            // Reverse the order of Spans 
            spanList.Reverse();

            //  Parallel function execution of CompensatingAction
            var tasks = new List<Task>();
            foreach (var span in spanList)
            {
                switch (span.SpanOpened.CompensatingAction)
                {
                    case HttpCompensatingAction:
                        {
                            tasks.Add(Task.Run(async () => {

                                var httpClient = _httpClientFactory.CreateClient();
                                httpClient.Timeout = TimeSpan.FromMilliseconds(30000);

                                var compensatingAction = (HttpCompensatingAction)span.SpanOpened.CompensatingAction;
                                var httpRequest = new HttpRequestMessage(HttpMethod.Post, compensatingAction.Url);

                                if (compensatingAction.PostData == null)
                                    compensatingAction.PostData = span.TraceId.ToString();

                                // Set content/body
                                httpRequest.Content = new StringContent(compensatingAction.PostData, Encoding.UTF8);

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
                                    var statusCode = response.StatusCode;
                                    
                                    logger.LogInformation("IsSuccessStatusCode: {IsSuccessStatusCode}", response.IsSuccessStatusCode);
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

                                return Task.CompletedTask;
                            }));
                           
                        };
                        break;

                    case AmqpCompensatingAction:
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                Thread.Sleep(1000);  // This is just a placeholder method
                            }));

                        };
                        break;

                    default:
                        // code block
                        break;

                }
            }

            await Task.WhenAll(tasks);
            //var results = tasks.Select(x => x.Result);
        }
    }
}

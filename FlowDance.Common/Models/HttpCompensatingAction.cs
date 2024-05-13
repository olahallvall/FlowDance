using System.Collections.Generic;

namespace FlowDance.Common.Models
{
    /// <summary>
    /// Compensating action for HTTP. Use the HTTP POST method when interacting with the endpoint as stated in the Url property.  
    /// </summary>
    public class HttpCompensatingAction : CompensatingAction
    {
        public string Url;
        public string PostData;
        public Dictionary<string, string> Headers;

        public HttpCompensatingAction() 
        {
        }
 
        public HttpCompensatingAction(string url)
        {
            Url = url;
        }

        public HttpCompensatingAction(string url, string postData)
        {
            Url = url;
            PostData = postData;
        }

        public HttpCompensatingAction(string url, string postData, Dictionary<string, string> headers)
        {
            Url = url;
            PostData = postData;
            Headers = headers;
        }
    }
}

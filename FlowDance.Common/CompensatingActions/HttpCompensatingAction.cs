using System.Collections.Generic;

namespace FlowDance.Common.CompensatingActions
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        public HttpCompensatingAction(string url)
        {
            Url = url;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        public HttpCompensatingAction(string url, string postData)
        {
            Url = url;
            PostData = postData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="headers"></param>
        public HttpCompensatingAction(string url, string postData, Dictionary<string, string> headers)
        {
            Url = url;
            PostData = postData;
            Headers = headers;
        }
    }
}

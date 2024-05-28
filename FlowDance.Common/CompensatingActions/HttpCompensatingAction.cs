using System.Collections.Generic;

namespace FlowDance.Common.CompensatingActions
{
    /// <summary>
    /// Compensating action for HTTP. Use the HTTP POST method when interacting with the endpoint as stated in the Url property.  
    /// </summary>
    public class HttpCompensatingAction : CompensatingAction
    {
        public string Url;
        public string CompensationData;
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
        /// <param name="compensationData"></param>
        public HttpCompensatingAction(string url, string compensationData)
        {
            Url = url;
            CompensationData = compensationData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="compensationData"></param>
        /// <param name="headers"></param>
        public HttpCompensatingAction(string url, string compensationData, Dictionary<string, string> headers)
        {
            Url = url;
            CompensationData = compensationData;
            Headers = headers;
        }
    }
}

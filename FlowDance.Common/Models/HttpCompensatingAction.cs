using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FlowDance.Common.Models
{
    public class HttpCompensatingAction : CompensatingAction
    {
        private readonly string _url;
        private HttpCompensatingAction()
        {
        }
        
        public HttpCompensatingAction(string url)
        {
            _url = url;
        }

        public string Url { get { return _url; } }
     
        public StringContent Content { get; set; }

        public HttpRequestHeaders Headers { get; set; }

        
    }
}

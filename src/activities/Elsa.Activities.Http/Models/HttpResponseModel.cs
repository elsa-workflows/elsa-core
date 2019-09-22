using System.Net;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Models
{
    public class HttpResponseModel
    {
        public HttpResponseModel()
        {
        }

        public HttpStatusCode StatusCode { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public byte[] Content { get; set; }
        public object ParsedContent { get; set; }
    }
}
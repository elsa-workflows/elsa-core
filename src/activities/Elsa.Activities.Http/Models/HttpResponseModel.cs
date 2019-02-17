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
        public string Content { get; set; }
        public object FormattedContent { get; set; }
    }
}
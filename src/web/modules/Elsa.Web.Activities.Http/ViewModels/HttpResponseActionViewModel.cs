using System.Net;
using Elsa.Web.Components.ViewModels;

namespace Elsa.Web.Activities.Http.ViewModels
{
    public class HttpResponseActionViewModel
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public ExpressionViewModel Body { get; set; }
        public ExpressionViewModel ContentType { get; set; }
        public ExpressionViewModel ResponseHeaders { get; set; }
    }
}
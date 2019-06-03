using Elsa.Core.Expressions;
using Elsa.Expressions;
using Elsa.Web.Components.ViewModels;

namespace Elsa.Web.Activities.Http.ViewModels
{
    public class HttpResponseActionViewModel
    {
        public HttpResponseActionViewModel()
        {
            StatusCode = 200;
            ContentType = new ExpressionViewModel("application/json", PlainTextEvaluator.SyntaxName);
        }
        
        public int StatusCode { get; set; }
        public ExpressionViewModel Body { get; set; }
        public ExpressionViewModel ContentType { get; set; }
        public ExpressionViewModel ResponseHeaders { get; set; }
    }
}
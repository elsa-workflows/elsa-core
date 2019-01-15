using Elsa.Activities.Http.Activities;
using Elsa.Web.Activities.Http.ViewModels;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Http.Display
{
    public class HttpResponseActionDisplay : ActivityDisplayDriver<HttpResponseAction, HttpResponseActionViewModel>
    {
        protected override void EditActivity(HttpResponseAction activity, HttpResponseActionViewModel model)
        {
            model.StatusCode = activity.StatusCode;
            model.Body = new ExpressionViewModel(activity.Body);
            model.ResponseHeaders = new ExpressionViewModel(activity.ResponseHeaders);
            model.ContentType = new ExpressionViewModel(activity.ContentType);
        }

        protected override void UpdateActivity(HttpResponseActionViewModel model, HttpResponseAction activity)
        {
            activity.StatusCode = model.StatusCode;
            activity.Body = model.Body.ToWorkflowExpression<string>();
            activity.ResponseHeaders = model.ResponseHeaders.ToWorkflowExpression<string>();
            activity.ContentType = model.ContentType.ToWorkflowExpression<string>();
        }
    }
}
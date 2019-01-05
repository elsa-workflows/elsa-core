using Elsa.Activities.Http.Activities;
using Elsa.Expressions;
using Elsa.Web.Activities.Http.ViewModels;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Http.Display
{
    public class HttpRequestActionDisplay : ActivityDisplayDriver<HttpRequestAction, HttpRequestActionViewModel>
    {
        protected override void EditActivity(HttpRequestAction activity, HttpRequestActionViewModel model)
        {
            model.Method = activity.Method;
            model.Body = new ExpressionViewModel(activity.Body);
            model.Url = activity.Url;
        }

        protected override void UpdateActivity(HttpRequestActionViewModel model, HttpRequestAction activity)
        {
            activity.Method = model.Method;
            activity.Body = model.Body.ToWorkflowExpression<string>();
            activity.Url = model.Url;
        }
    }
}
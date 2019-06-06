using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.Web.ViewModels;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Activities.Http.Web.Display
{
    public class HttpRequestActionDisplay : ActivityDisplayDriver<HttpRequestAction, HttpRequestActionViewModel>
    {
        protected override void EditActivity(HttpRequestAction activity, HttpRequestActionViewModel model)
        {
            model.Method = activity.Method;
            model.Body = new ExpressionViewModel(activity.Body);
            model.Url = new ExpressionViewModel(activity.Url);
            model.RequestHeaders = new ExpressionViewModel(activity.RequestHeaders);
            model.ContentType = new ExpressionViewModel(activity.ContentType);
            model.SupportedStatusCodes = string.Join(", ", activity.SupportedStatusCodes);
        }

        protected override void UpdateActivity(HttpRequestActionViewModel model, HttpRequestAction activity)
        {
            activity.Method = model.Method;
            activity.Body = model.Body.ToWorkflowExpression<string>();
            activity.Url = model.Url.ToWorkflowExpression<string>();
            activity.RequestHeaders = model.RequestHeaders.ToWorkflowExpression<string>();
            activity.ContentType = model.ContentType.ToWorkflowExpression<string>();
            activity.SupportedStatusCodes = new HashSet<int>(model.SupportedStatusCodes
                .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(int.Parse));
        }

        public HttpRequestActionDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}
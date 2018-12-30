using Flowsharp.Activities.Http.Activities;
using Flowsharp.Web.Abstractions.Drivers;
using Flowsharp.Web.Activities.Http.ViewModels;

namespace Flowsharp.Web.Activities.Http.Drivers
{
    public class HttpRequestTriggerDriver : ActivityDisplayDriver<HttpRequestTrigger, HttpRequestTriggerViewModel>
    {
        protected override void EditActivity(HttpRequestTrigger activity, HttpRequestTriggerViewModel model)
        {
            model.Path = activity.Path;
            model.Method = activity.Method;
        }

        protected override void UpdateActivity(HttpRequestTriggerViewModel model, HttpRequestTrigger activity)
        {
            activity.Path = model.Path;
            activity.Method = model.Method;
        }
    }
}
using Elsa.Activities.Http.Activities;
using Elsa.Web.Activities.Http.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Http.Display
{
    public class HttpRequestTriggerDisplay : ActivityDisplayDriver<HttpRequestTrigger, HttpRequestTriggerViewModel>
    {
        protected override void EditActivity(HttpRequestTrigger activity, HttpRequestTriggerViewModel model)
        {
            model.Path = activity.Path;
            model.Method = activity.Method;
            model.ReadContent = activity.ReadContent;
        }

        protected override void UpdateActivity(HttpRequestTriggerViewModel model, HttpRequestTrigger activity)
        {
            activity.Path = model.Path;
            activity.Method = model.Method;
            activity.ReadContent = model.ReadContent;
        }

        public HttpRequestTriggerDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}
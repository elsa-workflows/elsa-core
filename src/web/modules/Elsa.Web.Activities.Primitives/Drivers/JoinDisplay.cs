using Elsa.Activities.Primitives.Activities;
using Elsa.Web.Activities.Primitives.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Primitives.Drivers
{
    public class JoinDisplay : ActivityDisplayDriver<Join, JoinViewModel>
    {
        protected override void EditActivity(Join activity, JoinViewModel model)
        {
            model.Mode = activity.Mode;
        }

        protected override void UpdateActivity(JoinViewModel model, Join activity)
        {
            activity.Mode = model.Mode;
        }
    }
}
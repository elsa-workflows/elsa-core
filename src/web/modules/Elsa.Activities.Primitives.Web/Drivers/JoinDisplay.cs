using Elsa.Activities.Primitives.Activities;
using Elsa.Activities.Primitives.Web.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Activities.Primitives.Web.Drivers
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

        public JoinDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}
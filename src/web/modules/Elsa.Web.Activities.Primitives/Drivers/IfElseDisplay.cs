using Elsa.Activities.Primitives.Activities;
using Elsa.Web.Activities.Primitives.ViewModels;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Primitives.Drivers
{
    public class IfElseDisplay : ActivityDisplayDriver<IfElse, IfElseViewModel>
    {
        protected override void EditActivity(IfElse activity, IfElseViewModel model)
        {
            model.ConditionExpression = new ExpressionViewModel(activity.ConditionExpression);
        }

        protected override void UpdateActivity(IfElseViewModel model, IfElse activity)
        {
            activity.ConditionExpression = model.ConditionExpression.ToWorkflowExpression<bool>();
        }

        public IfElseDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}
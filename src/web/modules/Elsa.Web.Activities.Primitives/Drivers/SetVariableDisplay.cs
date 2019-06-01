using Elsa.Activities.Primitives.Activities;
using Elsa.Web.Activities.Primitives.ViewModels;
using Elsa.Web.Components.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Primitives.Drivers
{
    public class SetVariableDisplay : ActivityDisplayDriver<SetVariable, SetVariableViewModel>
    {
        protected override void EditActivity(SetVariable activity, SetVariableViewModel model)
        {
            model.VariableName = activity.VariableName;
            model.ValueExpression = new ExpressionViewModel(activity.ValueExpression);
        }

        protected override void UpdateActivity(SetVariableViewModel model, SetVariable activity)
        {
            activity.VariableName = model.VariableName;
            activity.ValueExpression = model.ValueExpression.ToWorkflowExpression<object>();
        }

        public SetVariableDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}
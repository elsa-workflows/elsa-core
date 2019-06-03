using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Web.ViewModels;
using Elsa.Expressions;
using Elsa.Web.Drivers;

namespace Elsa.Activities.Console.Web.Drivers
{
    public class WriteLineDisplay : ActivityDisplayDriver<WriteLine, WriteLineViewModel>
    {
        protected override void EditActivity(WriteLine activity, WriteLineViewModel model)
        {
            model.TextExpression = activity.TextExpression.Expression;
        }

        protected override void UpdateActivity(WriteLineViewModel model, WriteLine activity)
        {
            activity.TextExpression = new WorkflowExpression<string>(activity.TextExpression.Syntax, model.TextExpression);
        }

        public WriteLineDisplay(IActivityDesignerStore designerStore) : base(designerStore)
        {
        }
    }
}
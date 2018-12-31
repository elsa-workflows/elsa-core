using Elsa.Activities.Console.Activities;
using Elsa.Expressions;
using Elsa.Web.Activities.Console.ViewModels;
using Elsa.Web.Drivers;

namespace Elsa.Web.Activities.Console.Drivers
{
    public class WriteLineDriver : ActivityDisplayDriver<WriteLine, WriteLineViewModel>
    {
        protected override void EditActivity(WriteLine activity, WriteLineViewModel model)
        {
            model.TextExpression = activity.TextExpression.Expression;
        }

        protected override void UpdateActivity(WriteLineViewModel model, WriteLine activity)
        {
            activity.TextExpression = new WorkflowExpression<string>(activity.TextExpression.Syntax, model.TextExpression);
        }
    }
}
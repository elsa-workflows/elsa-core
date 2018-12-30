using Flowsharp.Activities.Console.Activities;
using Flowsharp.Expressions;
using Flowsharp.Web.Abstractions.Drivers;
using Flowsharp.Web.Activities.Console.ViewModels;

namespace Flowsharp.Web.Activities.Console.Drivers
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
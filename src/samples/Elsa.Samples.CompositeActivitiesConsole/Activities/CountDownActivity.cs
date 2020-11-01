using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Services;

namespace Elsa.Samples.CompositeActivitiesConsole.Activities
{
    public class CountDownActivity : CompositeActivity
    {
        public override void Build(IWorkflowBuilder workflowBuilder)
        {
            workflowBuilder
                .WriteLine("3!")
                .WriteLine("2!")
                .WriteLine("1!");
        }
    }
}
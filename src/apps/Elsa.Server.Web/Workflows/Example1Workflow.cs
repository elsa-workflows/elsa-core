using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows;
using ElsaServer.Activities;

namespace ElsaServer.Workflows
{
    public class Example1Workflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            var Number1 = new Variable<int>(1);
            var Number2 = new Variable<int>(2);
            var result = new Variable<int>();

            var start = new Start();
            var firstActivity = new FirstActivity();
            var console0 = new WriteLine(context => "Start!!!!!!!!!!!!!!! the sum " + Number1.Get(context) + "+" + Number2.Get(context));
            var console1 = new WriteLine(context => "Initial Value-->" + result.Get(context));
            var Example1Activity = new Example1Activity(Number1, Number2, result);
            var console2 = new WriteLine(context => "Result-->" + result.Get(context));
            var end = new End();

            builder.Root = new Flowchart  //Sequence || Flowchart
            {
                Variables = { Number1, Number2, result },
                Activities =
                   {
                        start,
                        firstActivity,
                        console0,
                        console1,
                        Example1Activity,
                        console2,
                        end
                    },

                Connections = {
                        new (start,firstActivity),
                        new (firstActivity,console0),
                        new (firstActivity,console1),
                        new (console1,Example1Activity),
                        new (Example1Activity,console2),
                        new (console2,end)
                    }
            };
        }

    }
}

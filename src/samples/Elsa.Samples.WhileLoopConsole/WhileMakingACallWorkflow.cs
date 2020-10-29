using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Timers;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.WhileLoopConsole
{
    /// <summary>
    /// This workflow prompts the user to enter an integer start value, then iterates back from that value to 0.
    /// The workflow also demonstrates retrieving runtime values such as user input. 
    /// </summary>
    public class WhileMakingACallWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("Simulating a phone call... ringgg ringg.")
                .While(context => !context.GetVariable<bool>("CallFinished"),
                    @while =>
                    {
                        @while
                            .TimerEvent(Duration.FromSeconds(5));
                    })
                .WriteLine("Workflow finished.");
        }
    }
}
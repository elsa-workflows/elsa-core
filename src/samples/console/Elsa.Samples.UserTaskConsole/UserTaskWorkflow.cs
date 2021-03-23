using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Activities.UserTask.Activities;

namespace Elsa.Samples.UserTaskConsole
{
    public class UserTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("Workflow started. Waiting for user action.").WithName("Start")
                .Then<UserTask>(
                    activity => activity.Set(x => x.Actions, new[] { "Accept", "Reject", "Needs Work" }),
                    userTask =>
                    {
                        userTask
                            .When("Accept")
                            .WriteLine("Great! Your work has been accepted.")
                            .ThenNamed("Exit");

                        userTask
                            .When("Reject")
                            .WriteLine("Sorry! Your work has been rejected.")
                            .ThenNamed("Exit");
                        
                        userTask
                            .When("Needs Work")
                            .WriteLine("So close! Your work needs a little bit more work.")
                            .ThenNamed("Exit");
                    }
                )
                .WriteLine("Workflow finished.").WithName("Exit");
        }
    }
}
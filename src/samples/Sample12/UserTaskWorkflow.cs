using Elsa.Activities.Console.Activities;
using Elsa.Activities.UserTask.Activities;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample12
{
    public class UserTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(activity => activity.TextExpression = new LiteralExpression("Workflow started. Waiting for user action."))
                .Then<UserTask>(
                    activity => activity.Actions = new[] { "Accept", "Reject", "Needs Work" },
                    userTask =>
                    {
                        userTask
                            .When("Accept")
                            .Then<WriteLine>(activity => activity.TextExpression = new LiteralExpression("Great! Your work has been accepted."))
                            .Then("Exit");

                        userTask.When("Reject")
                            .Then<WriteLine>(activity => activity.TextExpression = new LiteralExpression("Sorry! Your work has been rejected."))
                            .Then("Exit");

                        userTask.When("Needs Work")
                            .Then<WriteLine>(activity => activity.TextExpression = new LiteralExpression("So close! Your work needs a little bit more work."))
                            .Then("WaitUser");
                    },
                    "WaitUser"
                )
                .Add<WriteLine>(activity => activity.TextExpression = new LiteralExpression("Workflow finished."), "Exit");
        }
    }
}
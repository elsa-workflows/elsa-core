using Elsa;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;
using Sample25.Activities;

namespace Sample25
{
    public class DataProcessingWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(x => x.TextExpression = new LiteralExpression("Waiting for sensor input."))

                // Fork execution into two branches to wait for external stimuli from the two channels in parallel. 
                .Then<Fork>(
                    fork => fork.Branches = new[] { "Channel 1", "Channel 2" },
                    fork =>
                    {
                        fork
                            .When("Channel 1")
                            .Then<Channel>(x => x.SensorId = "Sensor1").WithName("Channel1")
                            .Then("Subtract"); // Connect to Subtract activity.

                        fork
                            .When("Channel 2")
                            .Then<Channel>(x => x.SensorId = "Sensor2").WithName("Channel2")
                            .Then("Subtract"); // Connect to Subtract activity.
                    })

                // Subtract the specified values.
                .Then<Subtract>(x => x.Values = new JavaScriptExpression<double[]>("[Channel1.Value, Channel2.Value]")).WithName("Subtract")

                // Calculate the absolute value of the subtraction.
                .Then<Absolute>(x => x.ValueExpression = new JavaScriptExpression<double>("(Subtract.Result)")).WithName("Absolute")

                // Compare the absolute value against a constant threshold, and write the appropriate output.
                .Then<IfElse>(
                    x => x.ConditionExpression = new JavaScriptExpression<bool>("(Absolute.Result) > 0.5"),
                    ifElse =>
                    {
                        ifElse
                            .When(OutcomeNames.False)
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Data does not exceed threshold (FALSE)"));

                        ifElse
                            .When(OutcomeNames.True)
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Data exceeds threshold (TRUE)"));
                    });
        }
    }
}

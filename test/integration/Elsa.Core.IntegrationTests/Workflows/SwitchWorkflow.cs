using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class SwitchWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("Start")
                .Then<Switch>(
                    @switch => @switch.WithCases("Case 1", "Case 2", "Case 3").WithValue(context => (string)context.WorkflowExecutionContext.Input!),
                    @switch =>
                    {
                        @switch
                            .When("Case 1")
                            .WriteLine("Case 1 executed", id: "WriteLine1");
                        
                        @switch
                            .When("Case 2")
                            .WriteLine("Case 2 executed", id: "WriteLine2");
                        
                        @switch
                            .When("Case 3")
                            .WriteLine("Case 3 executed", id: "WriteLine3");
                        
                        @switch
                            .When("Default")
                            .WriteLine("Default case executed", id: "WriteLineDefault");
                    });
        }
    }
}
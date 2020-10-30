using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Signaling;
using Elsa.Models;
using Elsa.Services.Models;
using Elsa.Testing.Shared.Helpers;
using Open.Linq.AsyncExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ForkJoinWorkflowTests : WorkflowsTestBase
    {
        public ForkJoinWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact(DisplayName = "JoinMode of WaitAll requires all signals to be triggered before continuing with Join.")]
        public async Task Test01()
        {
            var workflow = new ForkJoinWorkflow(Join.JoinMode.WaitAll);
            var workflowBlueprint = WorkflowBuilder.Build(workflow);
            var workflowInstance = await WorkflowRunner.RunWorkflowAsync(workflowBlueprint);

            bool GetActivityHasExecuted(string name) => (from entry in workflowInstance.ExecutionLog let activity = workflowBlueprint.Activities.First(x => x.Id == entry.ActivityId) where activity.Name == name select activity).Any();
            bool GetIsFinished() => GetActivityHasExecuted("Finished");

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.Status);
            Assert.False(GetIsFinished());

            // Trigger signal 1.
            workflowInstance = await TriggerSignalAsync(workflowBlueprint, workflowInstance, "Signal1");

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.Status);
            Assert.False(GetIsFinished());

            // Trigger signal 2.
            workflowInstance = await TriggerSignalAsync(workflowBlueprint, workflowInstance, "Signal2");

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.Status);
            Assert.False(GetIsFinished());

            // Trigger signal 3.
            workflowInstance = await TriggerSignalAsync(workflowBlueprint, workflowInstance, "Signal3");

            Assert.Equal(WorkflowStatus.Finished, workflowInstance.Status);
            Assert.True(GetIsFinished());
        }

        [Theory(DisplayName = "JoinMode of WaitAny continues with Join as soon as 1 branch executed.")]
        [InlineData("Signal1")]
        [InlineData("Signal2")]
        [InlineData("Signal3")]
        public async Task Test02(string signal)
        {
            var workflow = new ForkJoinWorkflow(Join.JoinMode.WaitAny);
            var workflowBlueprint = WorkflowBuilder.Build(workflow);
            var workflowInstance = await WorkflowRunner.RunWorkflowAsync(workflowBlueprint);

            bool GetActivityHasExecuted(string name) => (from entry in workflowInstance.ExecutionLog let activity = workflowBlueprint.Activities.First(x => x.Id == entry.ActivityId) where activity.Name == name select activity).Any();
            bool GetIsFinished() => GetActivityHasExecuted("Finished");

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.Status);
            Assert.False(GetIsFinished());

            // Trigger signal.
            workflowInstance = await TriggerSignalAsync(workflowBlueprint, workflowInstance, signal);
            
            Assert.Equal(WorkflowStatus.Finished, workflowInstance.Status);
            Assert.True(GetIsFinished());
        }

        private async Task<WorkflowInstance> TriggerSignalAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string signal)
        {
            var signaled = await WorkflowSelector.GetTriggersAsync<SignaledTrigger>(x => x.Signal == signal).First();
            var triggeredSignal = new TriggeredSignal(signal, null);
            return await WorkflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, signaled.Id, triggeredSignal);
        }
    }
}
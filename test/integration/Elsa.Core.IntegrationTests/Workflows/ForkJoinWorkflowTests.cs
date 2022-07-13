using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Signaling;
using Elsa.Activities.Signaling.Models;
using Elsa.Models;
using Elsa.Persistence.Specifications.WorkflowExecutionLogRecords;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using Elsa.Testing.Shared.Unit;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ForkJoinWorkflowTests : WorkflowsUnitTestBase
    {
        public ForkJoinWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact(DisplayName = "JoinMode of WaitAll requires all signals to be triggered before continuing with Join.")]
        public async Task Test01()
        {
            var workflow = new ForkJoinWorkflow(Join.JoinMode.WaitAll);
            var workflowBlueprint = WorkflowBuilder.Build(workflow);
            var workflowResult = await WorkflowStarter.StartWorkflowAsync(workflowBlueprint);
            var workflowInstance = workflowResult.WorkflowInstance!;

            async Task<bool> GetActivityHasExecutedAsync(string name)
            {
                var activity = workflowBlueprint.Activities.First(x => x.Name == name);
                var entry = await WorkflowExecutionLogStore.FindAsync(new ActivityIdSpecification(activity.Id));
                return entry != null;
            }
            
            Task<bool> GetIsFinishedAsync() => GetActivityHasExecutedAsync("Finished");

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.WorkflowStatus);
            Assert.False(await GetIsFinishedAsync());

            // Trigger signal 1.
            workflowInstance = await TriggerSignalAsync(workflowBlueprint, workflowInstance, "Signal1");

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.WorkflowStatus);
            Assert.False(await GetIsFinishedAsync());

            // Trigger signal 2.
            workflowInstance = await TriggerSignalAsync(workflowBlueprint, workflowInstance, "Signal2");

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.WorkflowStatus);
            Assert.False(await GetIsFinishedAsync());

            // Trigger signal 3.
            workflowInstance = await TriggerSignalAsync(workflowBlueprint, workflowInstance, "Signal3");

            Assert.Equal(WorkflowStatus.Finished, workflowInstance.WorkflowStatus);
            Assert.True(await GetIsFinishedAsync());
        }

        [Theory(DisplayName = "JoinMode of WaitAny continues with Join as soon as 1 branch executed.")]
        [InlineData("Signal1")]
        [InlineData("Signal2")]
        [InlineData("Signal3")]
        public async Task Test02(string signal)
        {
            var workflow = new ForkJoinWorkflow(Join.JoinMode.WaitAny);
            var workflowBlueprint = WorkflowBuilder.Build(workflow);
            var workflowResult = await WorkflowStarter.StartWorkflowAsync(workflowBlueprint);
            var workflowInstance = workflowResult.WorkflowInstance!;
            
            async Task<bool> GetActivityHasExecutedAsync(string name)
            {
                var activity = workflowBlueprint!.Activities.First(x => x.Name == name);
                var entry = await WorkflowExecutionLogStore.FindAsync(new ActivityIdSpecification(activity.Id));
                return entry != null;
            }
            
            Task<bool> GetIsFinishedAsync() => GetActivityHasExecutedAsync("Finished");

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.WorkflowStatus);
            Assert.False(await GetIsFinishedAsync());

            // Trigger signal.
            workflowInstance = await TriggerSignalAsync(workflowBlueprint, workflowInstance, signal);
            
            Assert.Equal(WorkflowStatus.Finished, workflowInstance.WorkflowStatus);
            Assert.True(await GetIsFinishedAsync());
        }
        
        [Fact(DisplayName = "Normal join should not eagerly clear blocking activities")]
        public async Task Test03()
        {
            var workflow = new ForkEagerJoinWorkflow(false, false);
            var workflowBlueprint = WorkflowBuilder.Build(workflow);
            var workflowResult = await WorkflowStarter.StartWorkflowAsync(workflowBlueprint);
            var workflowInstance = workflowResult.WorkflowInstance!;

            Assert.Equal(2, workflowInstance.BlockingActivities.Count);
        }
        
        [Fact(DisplayName = "Eager join should clear all blocking activities")]
        public async Task Test04()
        {
            var workflow = new ForkEagerJoinWorkflow(true, false);
            var workflowBlueprint = WorkflowBuilder.Build(workflow);
            var workflowResult = await WorkflowStarter.StartWorkflowAsync(workflowBlueprint);
            var workflowInstance = workflowResult.WorkflowInstance!;

            Assert.Single(workflowInstance.BlockingActivities);
        }
        
        [Fact(DisplayName = "Eager join should clear blocking and scheduled activities")]
        public async Task Test05()
        {
            var workflow = new ForkEagerJoinWorkflow(true, true);
            var workflowBlueprint = WorkflowBuilder.Build(workflow);
            var workflowResult = await WorkflowStarter.StartWorkflowAsync(workflowBlueprint);
            var workflowInstance = workflowResult.WorkflowInstance!;

            Assert.Single(workflowInstance.BlockingActivities);
        }
        
        [Fact(DisplayName = "Normal join should not clear scheduled activities")]
        public async Task Test06()
        {
            var workflow = new ForkEagerJoinWorkflow(false, true);
            var workflowBlueprint = WorkflowBuilder.Build(workflow);
            var workflowResult = await WorkflowStarter.StartWorkflowAsync(workflowBlueprint);
            var workflowInstance = workflowResult.WorkflowInstance!;

            Assert.Equal(2, workflowInstance.BlockingActivities.Count);
        }

        private async Task<WorkflowInstance> TriggerSignalAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string signal)
        {
            var workflowExecutionContext = new WorkflowExecutionContext(ServiceScope.ServiceProvider, workflowBlueprint, workflowInstance);
            var workflowBlueprintWrapper = new WorkflowBlueprintWrapper(workflowBlueprint, workflowExecutionContext);
            var activities = workflowBlueprintWrapper.Activities.Where(x => x.ActivityBlueprint.Type == nameof(SignalReceived));
            var blockingActivityIds = workflowInstance.BlockingActivities.Where(x => x.ActivityType == nameof(SignalReceived)).Select(x => x.ActivityId).ToList();
            var receiveSignalActivities = activities.Where(x => blockingActivityIds.Contains(x.ActivityBlueprint.Id)).ToList();
            var receiveSignal = receiveSignalActivities.Single(activity => workflowBlueprintWrapper.GetActivity<SignalReceived>(activity.ActivityBlueprint.Id)!.EvaluatePropertyValueAsync(x => x.Signal).GetAwaiter().GetResult() == signal);
            
            var triggeredSignal = new Signal(signal);
            await WorkflowStorageService.UpdateInputAsync(workflowInstance, new WorkflowInput(triggeredSignal));
            var result = await WorkflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, receiveSignal.ActivityBlueprint.Id);
            return result.WorkflowInstance!;
        }
    }
}
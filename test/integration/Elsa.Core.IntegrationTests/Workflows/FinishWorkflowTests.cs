using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Elsa.Models;
using Elsa.Providers.WorkflowStorage;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Unit;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class FinishWorkflowTests : WorkflowsUnitTestBase
    {
        public FinishWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory(DisplayName = "Sets the workflow status of the workflow instance to WorkflowStatus.Finished after RunWorkflowAsync is used"), AutoMoqData]
        public async Task RunWorkflowAsyncShouldSetWorkflowStatusToFinished(FinishWorkflow sut)
        {
            var runWorkflowResult = await WorkflowBuilderAndStarter.BuildAndStartWorkflowAsync(sut);
            var workflowInstance = runWorkflowResult.WorkflowInstance!;

            Assert.Equal(WorkflowStatus.Finished, workflowInstance.WorkflowStatus);
        }

        [Theory(DisplayName = "Sets the output of the workflow instance to a FinishOutput which contains the expected output after RunWorkflowAsync is used"), AutoMoqData]
        public async Task RunWorkflowAsyncShouldReturnExpectedOutput([Frozen] object expectedOutput, FinishWorkflow sut)
        {
            var runWorkflowResult = await WorkflowBuilderAndStarter.BuildAndStartWorkflowAsync(sut);
            var workflowInstance = runWorkflowResult.WorkflowInstance!;
            var actualOutputReference = workflowInstance.Output!;
            var actualOutput = (FinishOutput)(await WorkflowStorageService.LoadAsync(actualOutputReference.ProviderName, new WorkflowStorageContext(workflowInstance, workflowInstance.LastExecutedActivityId!), "Output"))!;

            Assert.Same(expectedOutput, actualOutput.Output);
        }
    }
}
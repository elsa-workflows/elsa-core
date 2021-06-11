using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Testing.Shared.Unit;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class BasicWorkflowTests : WorkflowsUnitTestBase
    {
        public BasicWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact(DisplayName = "Runs simple workflow.")]
        public async Task Test01()
        {
            var runWorkflowResult = await WorkflowBuilderAndStarter.BuildAndStartWorkflowAsync<BasicWorkflow>();
            var workflowInstance = runWorkflowResult.WorkflowInstance!;

            Assert.Equal(WorkflowStatus.Finished, workflowInstance.WorkflowStatus);
        }
    }
}
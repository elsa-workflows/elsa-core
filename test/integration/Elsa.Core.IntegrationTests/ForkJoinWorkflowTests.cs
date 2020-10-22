using System.Threading.Tasks;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Models;
using Elsa.Testing.Shared.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests
{
    public class ForkJoinWorkflowTests : WorkflowsUnitTestBase
    {
        public ForkJoinWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact(DisplayName = "Runs fork and join workflow.")]
        public async Task Test01()
        {
            var workflowInstance = await WorkflowHost.RunWorkflowAsync<ForkJoinWaitAllWorkflow>();

            Assert.Equal(WorkflowStatus.Completed, workflowInstance.Status);
        }
    }
}
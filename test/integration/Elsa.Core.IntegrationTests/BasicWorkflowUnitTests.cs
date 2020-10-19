using System.Threading.Tasks;
using Elsa.Core.IntegrationTests.Helpers;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Models;
using Elsa.Testing.Shared.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests
{
    public class BasicWorkflowUnitTests : WorkflowsUnitTestBase
    {
        public BasicWorkflowUnitTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact(DisplayName = "Runs simple workflow.")]
        public async Task Test01()
        {
            var workflowInstance = await WorkflowHost.RunWorkflowAsync<BasicWorkflow>();

            Assert.Equal(WorkflowStatus.Completed, workflowInstance.Status);
        }
    }
}
using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class SwitchWorkflowTests : WorkflowsTestBase
    {
        public SwitchWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory(DisplayName = "Runs Switch")]
        [InlineData("Case 1", "WriteLine1")]
        [InlineData("Case 2", "WriteLine2")]
        [InlineData("Case 3", "WriteLine3")]
        [InlineData("Different Case", "WriteLineDefault")]
        public async Task Test01(string input, string expectedActivityId)
        {
            var workflowInstance = await WorkflowRunner.RunWorkflowAsync<SwitchWorkflow>(input: input);
            var executedActivityIds = workflowInstance.ExecutionLog.Select(x => x.ActivityId).ToList();

            Assert.Contains(expectedActivityId, executedActivityIds);
        }
    }
}
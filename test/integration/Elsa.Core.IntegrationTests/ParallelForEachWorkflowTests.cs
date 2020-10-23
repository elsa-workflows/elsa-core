using System.Linq;
using System.Threading.Tasks;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Models;
using Elsa.Testing.Shared.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests
{
    public class ParallelForEachWorkflowTests : WorkflowsUnitTestBase
    {
        public ParallelForEachWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        
        [Fact(DisplayName = "Runs Parallel For Each workflow.")]
        public async Task Test01()
        {
            var items = Enumerable.Range(1, 10).Select(x => $"Item {x}").ToList();
            var workflow = new ParallelForEachWorkflow(items);
            var workflowInstance = await WorkflowRunner.RunWorkflowAsync(workflow);
            var iterationLogs = workflowInstance.ExecutionLog.Where(x => x.ActivityId == "WriteLine").ToList();

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.Status);
            Assert.Equal(items.Count, iterationLogs.Count);
        }
    }
}
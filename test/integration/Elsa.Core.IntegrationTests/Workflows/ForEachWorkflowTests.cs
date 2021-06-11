using System.Linq;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Testing.Shared.Unit;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ForEachWorkflowTests : WorkflowsUnitTestBase
    {
        public ForEachWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact(DisplayName = "Runs one iteration at a time, blocking the Iterate branch when an activity is blocking.")]
        public async Task Test01()
        {
            var items = Enumerable.Range(1, 10).Select(x => $"Item {x}").ToList();
            var workflow = new ForEachWorkflow(items);
            var runWorkflowResult = await WorkflowBuilderAndStarter.BuildAndStartWorkflowAsync(workflow);
            var workflowInstance = runWorkflowResult.WorkflowInstance!;

            Assert.Equal(WorkflowStatus.Suspended, workflowInstance.WorkflowStatus);
        }
    }
}
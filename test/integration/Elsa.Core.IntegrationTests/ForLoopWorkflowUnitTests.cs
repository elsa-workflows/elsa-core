using System.Linq;
using System.Threading.Tasks;
using Elsa.Core.IntegrationTests.Helpers;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Testing.Shared.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests
{
    public class ForLoopWorkflowUnitTests : WorkflowsUnitTestBase
    {
        public ForLoopWorkflowUnitTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        
        [Fact(DisplayName = "Runs For loop workflow.")]
        public async Task Test01()
        {
            const int loopCount = 10;
            var workflow = new ForLoopWorkflow(loopCount);
            var workflowInstance = await WorkflowHost.RunWorkflowAsync(workflow);
            var iterationLogs = workflowInstance.ExecutionLog.Where(x => x.ActivityId == "WriteLine").ToList();

            Assert.Equal(loopCount, iterationLogs.Count);
        }
    }
}
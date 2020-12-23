using System.Threading.Tasks;
using AutoFixture;
using Elsa.Models;
using Elsa.Testing.Shared.Unit;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class FinishWorkflowTests : WorkflowsUnitTestBase
    {
        private readonly Fixture _fixture;

        public FinishWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _fixture = new Fixture();
        }
        
        [Fact(DisplayName = "Sets the workflow output to the specified value")]
        public async Task Test01()
        {
            var output = _fixture.Create<string>();
            var workflow = new FinishWorkflow(output);
            var workflowInstance = await WorkflowRunner.RunWorkflowAsync(workflow);

            Assert.Equal(WorkflowStatus.Finished, workflowInstance.WorkflowStatus);
            Assert.Equal(output, workflowInstance.Output);
        }
    }
}
// using System.Linq;
// using System.Threading.Tasks;
// using Elsa.Testing.Shared.Unit;
// using Xunit;
// using Xunit.Abstractions;
//
// namespace Elsa.Core.IntegrationTests.Workflows
// {
//     public class IfElseWorkflowTests : WorkflowsUnitTestBase
//     {
//         public IfElseWorkflowTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
//         {
//         }
//         
//         [Fact(DisplayName = "Runs IfElse twice")]
//         public async Task Test01()
//         {
//             var workflowInstance = await WorkflowRunner.RunWorkflowAsync<IfElseWorkflow>();
//             var iterationLogs = workflowInstance.ExecutionLog.Where(x => x.ActivityId == "IfElse").ToList();
//
//             Assert.Equal(2, iterationLogs.Count);
//         }
//     }
// }
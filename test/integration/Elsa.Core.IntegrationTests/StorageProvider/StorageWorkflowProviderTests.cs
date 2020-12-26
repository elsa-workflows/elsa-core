using System.Threading.Tasks;
using Elsa.Core.IntegrationTests.Helpers;
using Elsa.Models;
using Elsa.Testing.Shared.Unit;
using Microsoft.Extensions.DependencyInjection;
using Storage.Net.Blobs;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Core.IntegrationTests.StorageProvider
{
    public class StorageWorkflowProviderTests : WorkflowsUnitTestBase
    {
        public StorageWorkflowProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact(DisplayName = "Runs workflows provided by storage provider")]
        public async Task Test01()
        {
            const string workflowFileName = "hello-world-workflow.json";
            const string workflowName = "SampleWorkflow";
            var json = await AssetHelper.ReadAssetAsync(workflowFileName);
            var storage = ServiceProvider.GetRequiredService<IBlobStorage>();
            await storage.WriteTextAsync(workflowFileName, json);
            var workflowBlueprint = await WorkflowRegistry.GetWorkflowAsync(workflowName, VersionOptions.Published);
            var workflowInstance = await WorkflowRunner.RunWorkflowAsync(workflowBlueprint!);

            Assert.Equal(WorkflowStatus.Finished, workflowInstance.WorkflowStatus);
        }
    }
}
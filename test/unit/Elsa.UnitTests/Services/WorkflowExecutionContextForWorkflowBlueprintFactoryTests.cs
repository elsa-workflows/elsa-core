using System;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using Elsa.Services.Workflows;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Moq;
using Xunit;

namespace Elsa.Services
{
    public class WorkflowExecutionContextForWorkflowBlueprintFactoryTests
    {
        [Theory(DisplayName = "The CreateWorkflowExecutionContextAsync method returns an execution context using the blueprint, an instance and the service provider"), AutoMoqData]
        public async Task CreateWorkflowExecutionContextAsyncReturnsContextWithBlueprintInstanceAndServiceProvider([WithAutofixtureResolution] IServiceProvider serviceProvider,
            IWorkflowFactory workflowFactory,
            IWorkflowBlueprint workflowBlueprint,
            [OmitOnRecursion] WorkflowInstance instance)
        {
            var sut = new WorkflowExecutionContextForWorkflowBlueprintFactory(serviceProvider, workflowFactory);
            Mock.Get(workflowFactory)
                .Setup(x => x.InstantiateAsync(workflowBlueprint, default, default, default))
                .Returns(() => Task.FromResult(instance));

            var result = await sut.CreateWorkflowExecutionContextAsync(workflowBlueprint);

            Assert.Same(serviceProvider, result.ServiceProvider);
            Assert.Same(workflowBlueprint, result.WorkflowBlueprint);
            Assert.Same(instance, result.WorkflowInstance);
        }
    }
}
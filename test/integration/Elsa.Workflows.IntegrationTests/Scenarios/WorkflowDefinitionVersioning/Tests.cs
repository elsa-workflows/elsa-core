using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDefinitionVersioning;

public class Tests
{
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
    }

    [Fact]
    public async Task RevertVersionAsync_ShouldAllocateVersionAfterHighestStoredVersion()
    {
        const string definitionId = "test-definition";
        var store = _services.GetRequiredService<IWorkflowDefinitionStore>();
        var publisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        var definitions = new WorkflowDefinition[]
        {
            new()
            {
                Id = "v1",
                DefinitionId = definitionId,
                Version = 1,
                IsPublished = true,
                IsLatest = false
            },
            new()
            {
                Id = "v2",
                DefinitionId = definitionId,
                Version = 2,
                IsPublished = false,
                IsLatest = true
            },
            new()
            {
                Id = "v3",
                DefinitionId = definitionId,
                Version = 3,
                IsPublished = false,
                IsLatest = false
            }
        };

        await store.SaveManyAsync(definitions);

        var revertedDefinition = await publisher.RevertVersionAsync(definitionId, 1);

        Assert.Equal(4, revertedDefinition.Version);
    }
}

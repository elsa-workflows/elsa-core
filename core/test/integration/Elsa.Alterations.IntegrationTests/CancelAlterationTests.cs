using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Extensions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Alterations.IntegrationTests;

public sealed class CancelAlterationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private readonly IServiceProvider _services = new TestApplicationBuilder(output)
        .ConfigureElsa(elsa => elsa.UseAlterations())
        .AddWorkflow<CancelAlterationWorkflow>()
        .Build();

    public Task InitializeAsync() => _services.PopulateRegistriesAsync();
    public async Task DisposeAsync() => await ((IAsyncDisposable)_services).DisposeAsync();

    [Fact(DisplayName = "The Cancel alteration persists a suspended instance as Finished/Cancelled")]
    public async Task CancelAlterationPersistsFinishedCancelled()
    {
        // Arrange
        var client = await _services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync();
        var suspended = await client.CreateAndRunInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(CancelAlterationWorkflow), VersionOptions.Published)
        });
        Assert.Equal(WorkflowSubStatus.Suspended, suspended.SubStatus);

        // Act
        var results = await _services.GetRequiredService<IAlterationRunner>().RunAsync([client.WorkflowInstanceId], [new Cancel()]);

        // Assert
        Assert.True(Assert.Single(results).IsSuccessful);
        var state = await client.ExportStateAsync();
        Assert.Equal((WorkflowStatus.Finished, WorkflowSubStatus.Cancelled), (state.Status, state.SubStatus));
    }

    public class CancelAlterationWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder) => builder.Root = new Event("cancel-alteration-go");
    }
}

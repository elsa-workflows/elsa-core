using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Extensions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Alterations.IntegrationTests;

/// <summary>
/// Contains tests for migration.
/// </summary>
public class MigrationTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IAlterationRunner _alterationRunner;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationTests"/> class.
    /// </summary>
    public MigrationTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa => elsa.UseAlterations())
            .Build();
        _alterationRunner = _services.GetRequiredService<IAlterationRunner>();
        _workflowInstanceStore = _services.GetRequiredService<IWorkflowInstanceStore>();
    }

    /// <summary>
    /// This method tests the migration from version 1 to version 2.
    /// </summary>
    [Fact(DisplayName = "Migrating from version 1 to 2 succeeds")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflows.
        await _services.ImportWorkflowDefinitionAsync("Workflows/version-1.json");
        await _services.ImportWorkflowDefinitionAsync("Workflows/version-2.json");

        // Run version 1 workflow.
        var workflowState = await _services.RunWorkflowUntilEndAsync("my-workflow", versionOptions: VersionOptions.SpecificVersion(1));

        // Migrate to version 2.
        var alterations = new IAlteration[]
        {
            new Migrate
            {
                TargetVersion = 2
            }
        };
        var instanceIds = new[]
        {
            workflowState.Id
        };
        var migrationResult = await _alterationRunner.RunAsync(instanceIds, alterations);
        
        // Assert success.
        Assert.True(migrationResult.All(x => x.IsSuccessful));
        
        // Assert that the workflow instance is now at version 2.
        var workflowState2 = await _workflowInstanceStore.FindAsync(workflowState.Id);
        Assert.Equal(2, workflowState2!.Version);
    }
}
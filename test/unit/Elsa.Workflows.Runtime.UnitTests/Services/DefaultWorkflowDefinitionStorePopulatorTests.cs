using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DefaultWorkflowDefinitionStorePopulatorTests
{
    private readonly IWorkflowDefinitionStore _storeMock;
    private readonly DefaultWorkflowDefinitionStorePopulator _populator;
    private readonly List<WorkflowDefinition> _workflowDefinitionsInStore = new();

    public DefaultWorkflowDefinitionStorePopulatorTests()
    {
        _storeMock = Substitute.For<IWorkflowDefinitionStore>();
        _storeMock.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()).Returns(_workflowDefinitionsInStore);
        _populator = new(() => new List<IWorkflowsProvider>(),
            Substitute.For<ITriggerIndexer>(),
            _storeMock,
            Substitute.For<IActivitySerializer>(),
            Substitute.For<IPayloadSerializer>(),
            Substitute.For<ISystemClock>(),
            Substitute.For<IIdentityGraphService>(),
            Substitute.For<ITenantAccessor>(),
            Substitute.For<ILogger<DefaultWorkflowDefinitionStorePopulator>>());
    }

    [Fact(DisplayName = "When adding a new workflow it needs to be saved")]
    public async Task AddOrUpdateCoreAsync_NewWorkflowDefinition_AddsWorkflowDefinition()
    {
        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("a", 7, "1"),
            Publication = new(true, true)
        }, "Test", "Test");

        await _populator.AddAsync(workflow);
        
        await CheckAmountOfWorkflowsSaved(1);
        await CheckIfWorkflowWasSaved(workflow.Workflow.Identity, wd => wd.IsLatest);
    }

    [Fact(DisplayName = "When adding a workflow with the same version, ignore it")]
    public async Task AddOrUpdateCoreAsync_ExistingWorkflowDefinition_KeepsExistingWorkflow()
    {
        _workflowDefinitionsInStore.Add(new()
        {
            Id = "1",
            DefinitionId = "a",
            Version = 1,
            Inputs = new List<InputDefinition>
            {
                new()
                {
                    Name = "InputA"
                }
            }
        });

        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("a", 1, "1"),
            Inputs = new List<InputDefinition>
            {
                new()
                {
                    Name = "A"
                }
            }
        }, "Test", "Test");

        await _populator.AddAsync(workflow);
        
        await _storeMock.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    
    [Theory(DisplayName = "When adding a newer workflow version, add it as latest and update the publication settings of older workflows")]
    [InlineData(false, false, false, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, true, true, false)]
    public async Task AddOrUpdateCoreAsync_NewWorkflowDefinitionVersion_AddsWorkflowDefinition(bool workflowAddedIsLatest, bool workflowAddedIsPublished, bool expectedPublishedStateAdded, bool expectedPublishedStateExisting)
    {
        _workflowDefinitionsInStore.Add(new()
        {
            Id = "1",
            DefinitionId = "a",
            Version = 1,
            IsLatest = true,
            IsPublished = true
        });
        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("a", 2, "2"),
            Publication = new(workflowAddedIsLatest, workflowAddedIsPublished)
        }, "Test", "Test");

        await _populator.AddAsync(workflow);
        
        await CheckIfWorkflowWasSaved(wd => wd is { Id: "1", IsLatest: false } 
                                            && wd.IsPublished == expectedPublishedStateExisting);
        await CheckIfWorkflowWasSaved(workflow.Workflow.Identity, wd => wd.IsLatest && wd.IsPublished == expectedPublishedStateAdded);
    }

    [Fact(DisplayName = "When adding a newer workflow version, ignore the root Version number")]
    public async Task AddOrUpdateCoreAsync_NewWorkflowDefinitionVersion_IgnoreRootVersion()
    {
        _workflowDefinitionsInStore.Add(new()
        {
            Id = "2",
            DefinitionId = "a",
            Version = 2,
            IsLatest = true,
            IsPublished = true
        });

        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("a", 3, "1"),
            Version = 1,
            Publication = new(true, true)
        }, "Test", "Test");

        await _populator.AddAsync(workflow);

        await CheckIfWorkflowWasSaved(wd => wd is { Id: "2", IsLatest: false, IsPublished: false });
        await CheckIfWorkflowWasSaved(workflow.Workflow.Identity, wd => wd is { IsLatest: true, IsPublished: true });
    }

    [Fact(DisplayName = "When adding a workflow version with the same ID, ignore it")]
    public async Task AddOrUpdateCoreAsync_NewWorkflowDefinitionVersionWithSameId_IsIgnored()
    {
        _workflowDefinitionsInStore.Add(
            new()
            {
                Id = "1",
                DefinitionId = "a",
                Version = 1,
            });

        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("a", 2, "1")
        }, "Test", "Test");

        await _populator.AddAsync(workflow);
        
        await _storeMock.DidNotReceive()
            .SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "When adding a older workflow version, keep the current latest as latest")]
    public async Task AddOrUpdateCoreAsync_OlderVersionAsLatest_ExistingVersionShouldRemainLatest()
    {
        _workflowDefinitionsInStore.Add(
            new()
            {
                Id = "2",
                DefinitionId = "a",
                Version = 2,
                IsLatest = true
            });
        
        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("a", 1, "1"),
            Publication = new(true, true)
        }, "Test", "Test");

        await _populator.AddAsync(workflow);
        
        await _storeMock.DidNotReceive().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l =>
            l.Any(wd => wd.Id == "2")), Arg.Any<CancellationToken>());

        await CheckIfWorkflowWasSaved(workflow.Workflow.Identity, wd => wd.IsLatest == false);
    }

    [Fact(DisplayName = "When adding a newer workflow version, keep the current published version published")]
    public async Task AddOrUpdateCoreAsync_OlderVersionAsPublished_ShouldNotBeUpdated()
    {
        _storeMock.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowDefinition>
            {
                new()
                {
                    Id = "2",
                    DefinitionId = "a",
                    Version = 2,
                    IsPublished = true
                }
            });

        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("a", 1, "1"),
            Publication = new(true, true)
        }, "Test", "Test");

        await _populator.AddAsync(workflow);
        await _storeMock.DidNotReceive().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l =>
            l.Any(wd => wd.Id == "2")), Arg.Any<CancellationToken>());
        await _storeMock.Received().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l =>
            l.Any(wd => wd.Id == "1" && wd.IsPublished == false)), Arg.Any<CancellationToken>());
    }

    private async Task CheckIfWorkflowWasSaved(WorkflowIdentity identity, Func<WorkflowDefinition, bool> predicate)
    {
        await _storeMock.Received(1).SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l =>
            l.Where(wd => wd.Id == identity.Id
                          && wd.DefinitionId == identity.DefinitionId
                          && wd.Version == identity.Version).Any(predicate)), Arg.Any<CancellationToken>());
    }

    private async Task CheckIfWorkflowWasSaved(Func<WorkflowDefinition, bool> predicate)
    {
        await _storeMock.Received(1).SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l =>
            l.Any(predicate)), Arg.Any<CancellationToken>());
    }

    private async Task CheckAmountOfWorkflowsSaved(int count)
    {
        await _storeMock.Received(count)
            .SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "PopulateStoreAsync imports workflows from current tenant")]
    public async Task PopulateStoreAsync_CurrentTenantWorkflows_ImportsWorkflows()
    {
        var currentTenantId = "tenant-1";

        var workflow1 = CreateMaterializedWorkflow("workflow-1", "id-1", currentTenantId);
        var workflow2 = CreateMaterializedWorkflow("workflow-2", "id-2", currentTenantId);

        var populator = CreatePopulatorWithTenant(currentTenantId, workflow1, workflow2);
        var result = await populator.PopulateStoreAsync();

        Assert.Equal(2, result.Count());
        await _storeMock.Received(2).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "PopulateStoreAsync skips workflows from other tenants")]
    public async Task PopulateStoreAsync_OtherTenantWorkflows_SkipsWorkflows()
    {
        var currentTenantId = "tenant-1";
        var otherTenantId = "tenant-2";

        var workflowCurrentTenant = CreateMaterializedWorkflow("workflow-1", "id-1", currentTenantId);
        var workflowOtherTenant = CreateMaterializedWorkflow("workflow-2", "id-2", otherTenantId);

        var populator = CreatePopulatorWithTenant(currentTenantId, workflowCurrentTenant, workflowOtherTenant);
        var result = await populator.PopulateStoreAsync().ToList();

        Assert.Single(result);
        Assert.Equal("workflow-1", result.First().DefinitionId);
        await _storeMock.Received(1).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "PopulateStoreAsync handles null/empty tenant IDs correctly")]
    [InlineData(null, null, true)]  // Both null - should import
    [InlineData("", "", true)]      // Both empty - should import
    [InlineData(null, "", true)]    // Normalized as same - should import
    [InlineData("tenant-1", null, false)]  // Different tenants - should skip
    [InlineData("tenant-1", "", false)]    // Different tenants - should skip
    public async Task PopulateStoreAsync_NullOrEmptyTenantIds_HandlesCorrectly(string? currentTenantId, string? workflowTenantId, bool shouldImport)
    {
        var workflow = CreateMaterializedWorkflow("workflow-1", "id-1", workflowTenantId);
        var populator = CreatePopulatorWithTenant(currentTenantId, workflow);
        var result = await populator.PopulateStoreAsync();

        if (shouldImport)
        {
            Assert.Single(result);
            await _storeMock.Received(1).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
        }
        else
        {
            Assert.Empty(result);
            await _storeMock.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
        }
    }

    private MaterializedWorkflow CreateMaterializedWorkflow(string definitionId, string id, string? tenantId)
    {
        return new(new()
        {
            Identity = new(definitionId, 1, id, tenantId),
            Publication = new(true, true)
        }, "Test", "TestProvider");
    }

    private DefaultWorkflowDefinitionStorePopulator CreatePopulatorWithTenant(string? tenantId, params MaterializedWorkflow[] workflows)
    {
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.Tenant.Returns(tenantId != null ? new Tenant { Id = tenantId } : null);
        tenantAccessor.TenantId.Returns(tenantId.NormalizeTenantId());

        var provider = Substitute.For<IWorkflowsProvider>();
        provider.Name.Returns("TestProvider");
        provider.GetWorkflowsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IEnumerable<MaterializedWorkflow>>(workflows));

        return new(
            () => new List<IWorkflowsProvider> { provider },
            Substitute.For<ITriggerIndexer>(),
            _storeMock,
            Substitute.For<IActivitySerializer>(),
            Substitute.For<IPayloadSerializer>(),
            Substitute.For<ISystemClock>(),
            Substitute.For<IIdentityGraphService>(),
            tenantAccessor,
            Substitute.For<ILogger<DefaultWorkflowDefinitionStorePopulator>>());
    }
}
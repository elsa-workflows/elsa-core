using System.Reflection;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DefaultWorkflowDefinitionStorePopulatorTests
{
    private readonly IWorkflowDefinitionStore _storeMock;
    private readonly DefaultWorkflowDefinitionStorePopulator _populator;
    private readonly List<WorkflowDefinition> _workflowDefinitionsInStore = new();

    private static readonly Dictionary<string, Action<WorkflowDefinition>> StoredValueChanges = new()
    {
        [nameof(WorkflowDefinition.DefinitionId)] = x => x.DefinitionId = "changed",
        [nameof(WorkflowDefinition.Version)] = x => x.Version = 99,
        [nameof(WorkflowDefinition.TenantId)] = x => x.TenantId = "changed",
        [nameof(WorkflowDefinition.Name)] = x => x.Name = "changed",
        [nameof(WorkflowDefinition.Description)] = x => x.Description = "changed",
        [nameof(WorkflowDefinition.ToolVersion)] = x => x.ToolVersion = new(9, 9),
        [nameof(WorkflowDefinition.IsLatest)] = x => x.IsLatest = !x.IsLatest,
        [nameof(WorkflowDefinition.IsPublished)] = x => x.IsPublished = !x.IsPublished,
        [nameof(WorkflowDefinition.IsReadonly)] = x => x.IsReadonly = !x.IsReadonly,
        [nameof(WorkflowDefinition.IsSystem)] = x => x.IsSystem = !x.IsSystem,
        [nameof(WorkflowDefinition.StringData)] = x => x.StringData = "changed",
        [nameof(WorkflowDefinition.OriginalSource)] = x => x.OriginalSource = "changed",
        [nameof(WorkflowDefinition.ProviderName)] = x => x.ProviderName = "changed",
        [nameof(WorkflowDefinition.MaterializerName)] = x => x.MaterializerName = "changed",
        [nameof(WorkflowDefinition.MaterializerContext)] = x => x.MaterializerContext = "changed",
        [nameof(WorkflowDefinition.Options)] = x => x.Options = new() { ActivityCategory = "changed" },
        [nameof(WorkflowDefinition.Variables)] = x => x.Variables = new List<Variable>(),
        [nameof(WorkflowDefinition.Inputs)] = x => x.Inputs = new List<InputDefinition>(),
        [nameof(WorkflowDefinition.Outputs)] = x => x.Outputs = new List<OutputDefinition>(),
        [nameof(WorkflowDefinition.Outcomes)] = x => x.Outcomes = new List<string>(),
        [nameof(WorkflowDefinition.CustomProperties)] = x => x.CustomProperties = new Dictionary<string, object>()
    };

    public DefaultWorkflowDefinitionStorePopulatorTests()
    {
        _storeMock = Substitute.For<IWorkflowDefinitionStore>();
        _storeMock.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(_workflowDefinitionsInStore);
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

    [Fact(DisplayName = "PopulateStoreAsync skips a non-CLR save that hits another tenant without an upgrade hint")]
    public async Task PopulateStoreAsync_CrossTenantIdCollision_LogsAndSkipsWithoutUpgradeHint()
    {
        var logger = new CapturingLogger();
        _storeMock
            .When(x => x.SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException(
                "A record with ID 'shared-v1' already exists in another tenant."));

        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("shared-workflow", 1, "shared-v1", Tenant.AgnosticTenantId),
            Publication = new(true, true)
        }, "Test", "TestProvider");
        var populator = CreatePopulatorWithTenant("tenant-b", logger, workflow);

        var result = (await populator.PopulateStoreAsync()).ToList();

        Assert.Empty(result);
        Assert.Contains(logger.Messages, message =>
            message.Contains("Skipping adding workflow", StringComparison.Ordinal)
            && message.Contains("already exists in another tenant", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message =>
            message.Contains("3.10.1", StringComparison.Ordinal)
            || message.Contains("upgrade step", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "PopulateStoreAsync restamps a tenant-declared CLR workflow stored as '*'")]
    public async Task PopulateStoreAsync_ExistingStarRow_DeclaredConcreteTenant_RestampsAndSaves()
    {
        _storeMock.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(new WorkflowDefinition
            {
                Id = "owned-v1",
                DefinitionId = "owned-workflow",
                Version = 1,
                TenantId = Tenant.AgnosticTenantId,
                ProviderName = "CLR"
            });

        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("owned-workflow", 1, "owned-v1", "tenant-a"),
            Publication = new(true, true)
        }, "CLR", "ClrWorkflowMaterializer");
        var populator = CreatePopulatorWithTenant("tenant-a", workflow);

        var result = (await populator.PopulateStoreAsync()).ToList();

        Assert.Single(result);
        await _storeMock.Received().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(definitions =>
            definitions.Any(definition =>
                definition.Id == "owned-v1"
                && definition.TenantId == "tenant-a")), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "PopulateStoreAsync restamps every CLR workflow, including those it then skips")]
    public async Task PopulateStoreAsync_OtherTenantClrWorkflow_RestampsBeforeSkip()
    {
        var restamper = Substitute.For<IClrWorkflowDefinitionTenantRestamper>();
        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("owned-workflow", 1, "owned-v1", "tenant-a"),
            Publication = new(true, true)
        }, "CLR", "ClrWorkflowMaterializer");
        var populator = CreatePopulatorWithTenant("tenant-b", restamper, workflow);

        var result = (await populator.PopulateStoreAsync()).ToList();

        Assert.Empty(result);
        await restamper.Received(1).RestampAsync("owned-workflow", 1, "tenant-a", Arg.Any<CancellationToken>());
        await _storeMock.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A failing CLR re-stamp is skipped so later workflows still populate, index, and reload")]
    public async Task PopulateAsync_MalformedClrRestamp_StillPopulatesIndexesAndReloads()
    {
        var restamper = Substitute.For<IClrWorkflowDefinitionTenantRestamper>();
        restamper
            .RestampAsync("bad-workflow", Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new InvalidOperationException("malformed Props"));
        restamper
            .RestampAsync("good-workflow", Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var triggerIndexer = Substitute.For<ITriggerIndexer>();
        var notificationSender = Substitute.For<INotificationSender>();
        var logger = new CapturingLogger();
        var bad = new MaterializedWorkflow(new()
        {
            Identity = new("bad-workflow", 1, "bad-v1", Tenant.AgnosticTenantId),
            Publication = new(true, true)
        }, "CLR", "ClrWorkflowMaterializer");
        var good = new MaterializedWorkflow(new()
        {
            Identity = new("good-workflow", 1, "good-v1", Tenant.AgnosticTenantId),
            Publication = new(true, true)
        }, "CLR", "ClrWorkflowMaterializer");
        var storePopulator = CreatePopulatorWithTenant("tenant-a", logger, restamper, triggerIndexer, bad, good);
        var registries = new DefaultRegistriesPopulator(
            storePopulator,
            Substitute.For<IActivityRegistryPopulator>(),
            notificationSender);

        await registries.PopulateAsync();

        await _storeMock.Received().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(definitions =>
            definitions.Any(definition => definition.DefinitionId == "good-workflow")), Arg.Any<CancellationToken>());
        await triggerIndexer.Received().IndexTriggersAsync(
            Arg.Is<WorkflowDefinition>(definition => definition.DefinitionId == "good-workflow"),
            Arg.Any<CancellationToken>());
        await notificationSender.Received().SendAsync(
            Arg.Is<WorkflowDefinitionsReloaded>(notification =>
                notification.ReloadedWorkflowDefinitions.Any(definition => definition.DefinitionId == "good-workflow")),
            Arg.Any<CancellationToken>());
        Assert.Contains(logger.Messages, message =>
            message.Contains("Skipping CLR tenant re-stamp", StringComparison.Ordinal)
            && message.Contains("bad-workflow", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Cancelled restamp cancellation propagates with no per-workflow error logs")]
    public async Task PopulateStoreAsync_CancelledToken_Restamp_PropagatesWithoutPerWorkflowLogs()
    {
        // Arrange
        var logger = new CapturingLogger();
        using var cts = new CancellationTokenSource();
        var restamper = Substitute.For<IClrWorkflowDefinitionTenantRestamper>();
        restamper
            .RestampAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });
        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("shared-workflow", 1, "shared-v1", Tenant.AgnosticTenantId),
            Publication = new(true, true)
        }, "CLR", "ClrWorkflowMaterializer");
        var populator = CreatePopulatorWithTenant("tenant-a", logger, restamper, workflow);

        // Act / Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => populator.PopulateStoreAsync(cts.Token));
        Assert.Empty(logger.Messages);
    }

    [Fact(DisplayName = "Cancelled store cancellation propagates with no per-workflow error logs")]
    public async Task PopulateStoreAsync_CancelledToken_StoreFind_PropagatesWithoutPerWorkflowLogs()
    {
        // Arrange
        var logger = new CapturingLogger();
        using var cts = new CancellationTokenSource();
        _storeMock
            .When(x => x.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });
        var workflow = new MaterializedWorkflow(new()
        {
            Identity = new("shared-workflow", 1, "shared-v1", Tenant.AgnosticTenantId),
            Publication = new(true, true)
        }, "Test", "TestProvider");
        var populator = CreatePopulatorWithTenant("tenant-a", logger, workflow);

        // Act / Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => populator.PopulateStoreAsync(cts.Token));
        Assert.Empty(logger.Messages);
    }

    [Fact(DisplayName = "A live-token store cancellation is logged and later workflows still populate")]
    public async Task PopulateStoreAsync_LiveToken_StoreCancellation_LogsAndContinues()
    {
        // Arrange
        var logger = new CapturingLogger();
        _storeMock
            .When(x => x.FindAsync(
                Arg.Is<WorkflowDefinitionFilter>(filter => filter.DefinitionId == "bad-workflow"),
                Arg.Any<CancellationToken>()))
            .Do(_ => throw new TaskCanceledException());
        var bad = new MaterializedWorkflow(new()
        {
            Identity = new("bad-workflow", 1, "bad-v1", Tenant.AgnosticTenantId),
            Publication = new(true, true)
        }, "Test", "TestProvider");
        var good = new MaterializedWorkflow(new()
        {
            Identity = new("good-workflow", 1, "good-v1", Tenant.AgnosticTenantId),
            Publication = new(true, true)
        }, "Test", "TestProvider");
        var populator = CreatePopulatorWithTenant("tenant-a", logger, bad, good);

        // Act
        var result = (await populator.PopulateStoreAsync()).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("good-workflow", result[0].DefinitionId);
        Assert.Contains(logger.Messages, message =>
            message.Contains("An error occurred while populating", StringComparison.Ordinal)
            && message.Contains("bad-workflow", StringComparison.Ordinal));
        await _storeMock.Received().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(definitions =>
            definitions.Any(definition => definition.DefinitionId == "good-workflow")), Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "The full constructor activates with or without a restamper")]
    [InlineData("container", false)]
    [InlineData("container", true)]
    [InlineData("create-instance", false)]
    [InlineData("create-instance", true)]
    [InlineData("get-service-or-create", false)]
    [InlineData("get-service-or-create", true)]
    [InlineData("create-factory", false)]
    [InlineData("create-factory", true)]
    public void CreatePopulator_UsesFullConstructor_WithOptionalRestamper(string path, bool registerRestamper)
    {
        // Arrange
        var restamper = Substitute.For<IClrWorkflowDefinitionTenantRestamper>();
        var services = new ServiceCollection();
        services.AddSingleton<Func<IEnumerable<IWorkflowsProvider>>>(() => []);
        services.AddSingleton(Substitute.For<ITriggerIndexer>());
        services.AddSingleton(Substitute.For<IWorkflowDefinitionStore>());
        services.AddSingleton(Substitute.For<IActivitySerializer>());
        services.AddSingleton(Substitute.For<IPayloadSerializer>());
        services.AddSingleton(Substitute.For<ISystemClock>());
        services.AddSingleton(Substitute.For<IIdentityGraphService>());
        services.AddSingleton(Substitute.For<ITenantAccessor>());
        services.AddSingleton(Substitute.For<ILogger<DefaultWorkflowDefinitionStorePopulator>>());
        if (registerRestamper)
        {
            services.AddSingleton(restamper);
        }

        if (path == "container")
        {
            services.AddSingleton<DefaultWorkflowDefinitionStorePopulator>();
        }

        using var provider = services.BuildServiceProvider();

        // Act
        var populator = path switch
        {
            "container" => provider.GetRequiredService<DefaultWorkflowDefinitionStorePopulator>(),
            "create-instance" => ActivatorUtilities.CreateInstance<DefaultWorkflowDefinitionStorePopulator>(provider),
            "get-service-or-create" => ActivatorUtilities.GetServiceOrCreateInstance<DefaultWorkflowDefinitionStorePopulator>(provider),
            "create-factory" => (DefaultWorkflowDefinitionStorePopulator)ActivatorUtilities.CreateFactory(typeof(DefaultWorkflowDefinitionStorePopulator), Type.EmptyTypes)(provider, null),
            _ => throw new ArgumentOutOfRangeException(nameof(path), path, null)
        };

        // Assert
        Assert.NotNull(populator);
        var assignedRestamper = GetClrTenantRestamper(populator);
        if (registerRestamper)
        {
            Assert.Same(restamper, assignedRestamper);
        }
        else
        {
            Assert.Null(assignedRestamper);
        }
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

    [Fact(DisplayName = "PopulateStoreAsync builds the workflows of a tenant-agnostic provider once across tenants")]
    public async Task PopulateStoreAsync_TenantAgnosticProvider_BuiltOnceAcrossTenants()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var provider = new TenantAgnosticProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));

        await CreatePopulatorWithTenant("tenant-1", provider, cache, CreateStore()).PopulateStoreAsync();
        await CreatePopulatorWithTenant("tenant-2", provider, cache, CreateStore()).PopulateStoreAsync();
        await CreatePopulatorWithTenant("tenant-3", provider, cache, CreateStore()).PopulateStoreAsync();

        Assert.Equal(1, provider.CallCount);
    }

    [Fact(DisplayName = "PopulateStoreAsync only looks up the stored definitions by ID and skips identical ones in a shared store")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderIdenticalRowsStored_SkipsImport()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var store = CreateStore();
        var provider = new TenantAgnosticProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));
        await CreatePopulatorWithTenant("tenant-1", provider, cache, store).PopulateStoreAsync();
        store.ClearReceivedCalls();
        var triggerIndexer = Substitute.For<ITriggerIndexer>();

        var result = (await CreatePopulatorWithTenant("tenant-2", provider, cache, store, triggerIndexer).PopulateStoreAsync()).ToList();

        var filter = Assert.Single(store.ReceivedCalls(), x => x.GetMethodInfo().Name == nameof(IWorkflowDefinitionStore.FindManyAsync)).GetArguments()[0];
        Assert.Equivalent(new WorkflowDefinitionFilter { Ids = ["id-1"] }, filter, strict: true);
        await store.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
        await triggerIndexer.Received(1).IndexTriggersAsync(Arg.Is<WorkflowDefinition>(x => x.Id == "id-1"), Arg.Any<CancellationToken>());
        Assert.Equal("id-1", Assert.Single(result).Id);
    }

    [Fact(DisplayName = "PopulateStoreAsync does not re-stamp a CLR workflow whose stored row is identical")]
    public async Task PopulateStoreAsync_ClrWorkflowIdenticalRowStored_DoesNotRestamp()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var store = CreateStore();
        var provider = new TenantAgnosticProviderStub(CreateClrMaterializedWorkflow());
        await CreatePopulatorWithTenant("tenant-1", provider, cache, store).PopulateStoreAsync();
        store.ClearReceivedCalls();
        var restamper = Substitute.For<IClrWorkflowDefinitionTenantRestamper>();

        await CreatePopulatorWithTenant("tenant-2", provider, cache, store, restamper: restamper).PopulateStoreAsync();

        await restamper.DidNotReceive().RestampAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "PopulateStoreAsync re-stamps a CLR workflow whose stored row carries another tenant")]
    public async Task PopulateStoreAsync_ClrWorkflowStoredUnderAnotherTenant_Restamps()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var store = CreateStore();
        var provider = new TenantAgnosticProviderStub(CreateClrMaterializedWorkflow());
        await CreatePopulatorWithTenant("tenant-1", provider, cache, store).PopulateStoreAsync();
        Assert.Single(store.Rows).TenantId = "tenant-2";
        store.ClearReceivedCalls();
        var restamper = Substitute.For<IClrWorkflowDefinitionTenantRestamper>();

        await CreatePopulatorWithTenant("tenant-2", provider, cache, store, restamper: restamper).PopulateStoreAsync();

        await restamper.Received(1).RestampAsync("workflow-1", 1, Tenant.AgnosticTenantId, Arg.Any<CancellationToken>());
        await store.Received(1).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
        Assert.Equal(Tenant.AgnosticTenantId, Assert.Single(store.Rows).TenantId);
    }

    [Fact(DisplayName = "PopulateStoreAsync imports every provider for every tenant through the public constructor")]
    public async Task PopulateStoreAsync_PublicConstructor_ImportsEveryProviderForEveryTenant()
    {
        var provider = new TenantAgnosticProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));
        var store = CreateStore();

        await CreatePublicConstructorPopulatorWithTenant("tenant-1", provider, store).PopulateStoreAsync();
        await CreatePublicConstructorPopulatorWithTenant("tenant-2", provider, store).PopulateStoreAsync();

        Assert.Equal(2, provider.CallCount);
        Assert.Single(store.Rows);
    }

    [Fact(DisplayName = "PopulateStoreAsync imports a tenant-agnostic provider into every tenant with its own store")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderSeparateStores_ImportedIntoEachStore()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var provider = new TenantAgnosticProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));
        var secondStore = CreateStore();
        var secondTriggerIndexer = Substitute.For<ITriggerIndexer>();
        await CreatePopulatorWithTenant("tenant-1", provider, cache, CreateStore()).PopulateStoreAsync();

        var result = await CreatePopulatorWithTenant("tenant-2", provider, cache, secondStore, secondTriggerIndexer).PopulateStoreAsync();

        Assert.Single(result);
        await secondStore.Received(1).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
        await secondTriggerIndexer.Received(1).IndexTriggersAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "PopulateStoreAsync imports a tenant-agnostic workflow again when the stored definition differs")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderDifferentRowStored_Reimported()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var store = CreateStore();
        var provider = new TenantAgnosticProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));
        await CreatePopulatorWithTenant("tenant-1", provider, cache, store).PopulateStoreAsync();
        store.Rows.Single().StringData = "changed";
        store.ClearReceivedCalls();

        await CreatePopulatorWithTenant("tenant-2", provider, cache, store).PopulateStoreAsync();

        await store.Received(1).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "PopulateStoreAsync still imports a tenant-agnostic provider into the next tenant when saving failed for the first")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderSaveFails_ImportedForNextTenant()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var provider = new TenantAgnosticProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));
        var failingStore = CreateStore();
        failingStore.SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>()).Returns(_ => throw new InvalidOperationException());
        var secondStore = CreateStore();

        await CreatePopulatorWithTenant("tenant-1", provider, cache, failingStore).PopulateStoreAsync();
        var result = await CreatePopulatorWithTenant("tenant-2", provider, cache, secondStore).PopulateStoreAsync();

        Assert.Single(result);
        await secondStore.Received(1).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "PopulateStoreAsync does not cache a failed build of a tenant-agnostic provider")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderThrows_BuiltAgainForNextTenant()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var builds = 0;
        var provider = new TenantAgnosticProviderStub
        {
            GetWorkflows = () => builds++ == 0 ? throw new InvalidOperationException() : [CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId)]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreatePopulatorWithTenant("tenant-1", provider, cache, CreateStore()).PopulateStoreAsync());
        var result = await CreatePopulatorWithTenant("tenant-2", provider, cache, CreateStore()).PopulateStoreAsync();

        Assert.Single(result);
    }

    [Fact(DisplayName = "PopulateStoreAsync keeps querying a tenant-agnostic provider that returned tenant-specific workflows")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderWithTenantSpecificWorkflows_QueriedForEveryTenant()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var provider = new TenantAgnosticProviderStub(
            CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId),
            CreateMaterializedWorkflow("workflow-2", "id-2", "tenant-2"));

        await CreatePopulatorWithTenant("tenant-1", provider, cache, CreateStore()).PopulateStoreAsync();
        var result = await CreatePopulatorWithTenant("tenant-2", provider, cache, CreateStore()).PopulateStoreAsync();

        Assert.Contains(result, x => x.DefinitionId == "workflow-2");
        Assert.Equal(2, provider.CallCount);
    }

    [Fact(DisplayName = "PopulateStoreAsync imports the workflows of an ordinary provider for every tenant")]
    public async Task PopulateStoreAsync_OrdinaryProvider_ImportedForEveryTenant()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var provider = CreateProvider<IWorkflowsProvider>(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));

        await CreatePopulatorWithTenant("tenant-1", provider, cache).PopulateStoreAsync();
        await CreatePopulatorWithTenant("tenant-2", provider, cache).PopulateStoreAsync();

        await provider.Received(2).GetWorkflowsAsync(Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "PopulateStoreAsync skips every version of a tenant-agnostic workflow that is stored the way an import leaves it")]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task PopulateStoreAsync_TenantAgnosticProviderSeveralVersionsStored_SkipsImport(bool newestFirst, bool newestPublished)
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var store = CreateStore();
        var olderVersion = CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId);
        var newerVersion = CreateMaterializedWorkflow("workflow-1", "id-2", Tenant.AgnosticTenantId, 2, newestPublished);
        var provider = new TenantAgnosticProviderStub(newestFirst ? [newerVersion, olderVersion] : [olderVersion, newerVersion]);
        await CreatePopulatorWithTenant("tenant-1", provider, cache, store).PopulateStoreAsync();
        store.ClearReceivedCalls();
        var triggerIndexer = Substitute.For<ITriggerIndexer>();

        var result = await CreatePopulatorWithTenant("tenant-2", provider, cache, store, triggerIndexer).PopulateStoreAsync();

        Assert.Equal(2, result.Count());
        await store.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
        await triggerIndexer.Received(1).IndexTriggersAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "PopulateStoreAsync imports a tenant-agnostic workflow again when any compared value of the stored definition differs")]
    [InlineData(null)]
    [InlineData(nameof(WorkflowDefinition.DefinitionId))]
    [InlineData(nameof(WorkflowDefinition.Version))]
    [InlineData(nameof(WorkflowDefinition.TenantId))]
    [InlineData(nameof(WorkflowDefinition.Name))]
    [InlineData(nameof(WorkflowDefinition.Description))]
    [InlineData(nameof(WorkflowDefinition.ToolVersion))]
    [InlineData(nameof(WorkflowDefinition.IsLatest))]
    [InlineData(nameof(WorkflowDefinition.IsPublished))]
    [InlineData(nameof(WorkflowDefinition.IsReadonly))]
    [InlineData(nameof(WorkflowDefinition.IsSystem))]
    [InlineData(nameof(WorkflowDefinition.StringData))]
    [InlineData(nameof(WorkflowDefinition.OriginalSource))]
    [InlineData(nameof(WorkflowDefinition.ProviderName))]
    [InlineData(nameof(WorkflowDefinition.MaterializerName))]
    [InlineData(nameof(WorkflowDefinition.MaterializerContext))]
    [InlineData(nameof(WorkflowDefinition.Options))]
    [InlineData(nameof(WorkflowDefinition.Variables))]
    [InlineData(nameof(WorkflowDefinition.Inputs))]
    [InlineData(nameof(WorkflowDefinition.Outputs))]
    [InlineData(nameof(WorkflowDefinition.Outcomes))]
    [InlineData(nameof(WorkflowDefinition.CustomProperties))]
    public async Task PopulateStoreAsync_TenantAgnosticProviderStoredValueDiffers_ReimportedUnlessIdentical(string? changedMember)
    {
        var store = CreateStore();
        var payloadSerializer = CreateDescribingPayloadSerializer();
        var provider = new TenantAgnosticProviderStub(CreateRichMaterializedWorkflow());
        await CreatePopulatorWithTenant("tenant-1", provider, new(), store, payloadSerializer: payloadSerializer).PopulateStoreAsync();
        var storedDefinition = store.Rows.Single();

        if (changedMember != null)
        {
            StoredValueChanges[changedMember](storedDefinition);
        }

        store.ClearReceivedCalls();

        await CreatePopulatorWithTenant("tenant-2", provider, new(), store, payloadSerializer: payloadSerializer).PopulateStoreAsync();

        await store.Received(changedMember == null ? 0 : 1).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "PopulateStoreAsync imports a tenant-agnostic workflow again when its stored definition cannot be serialized for comparison")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderStoredDefinitionNotSerializable_Reimported()
    {
        var store = CreateStore();
        var payloadSerializer = CreateDescribingPayloadSerializer();
        var provider = new TenantAgnosticProviderStub(CreateRichMaterializedWorkflow());
        await CreatePopulatorWithTenant("tenant-1", provider, new(), store, payloadSerializer: payloadSerializer).PopulateStoreAsync();
        var storedVariables = new List<Variable> { new("Variable") };
        store.Rows.Single().Variables = storedVariables;
        payloadSerializer.Serialize(Arg.Is<object>(x => x.GetType().GetProperty(nameof(WorkflowDefinition.Variables)) != null && x.GetType().GetProperty(nameof(WorkflowDefinition.Variables))!.GetValue(x) == storedVariables)).Returns(_ => throw new InvalidOperationException());
        store.ClearReceivedCalls();

        var result = await CreatePopulatorWithTenant("tenant-2", provider, new(), store, payloadSerializer: payloadSerializer).PopulateStoreAsync();

        Assert.Equal("id-1", Assert.Single(result).Id);
        await store.Received(1).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "PopulateStoreAsync still imports the other workflows when one workflow of a tenant-agnostic provider cannot be serialized")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderWorkflowNotSerializable_ImportsOtherWorkflows()
    {
        var failingWorkflow = CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId);
        var activitySerializer = Substitute.For<IActivitySerializer>();
        activitySerializer.Serialize(failingWorkflow.Workflow.Root).Returns(_ => throw new InvalidOperationException());
        var provider = new TenantAgnosticProviderStub(failingWorkflow, CreateMaterializedWorkflow("workflow-2", "id-2", Tenant.AgnosticTenantId));
        var store = CreateStore();

        var result = await CreatePopulatorWithTenant("tenant-1", provider, new(), store, activitySerializer: activitySerializer).PopulateStoreAsync();

        Assert.Equal("id-2", Assert.Single(result).Id);
        Assert.Equal("id-2", Assert.Single(store.Rows).Id);
    }

    [Fact(DisplayName = "PopulateStoreAsync imports every workflow of a tenant-agnostic provider when looking up the stored definitions fails")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderLookupFails_ImportsEveryWorkflow()
    {
        var provider = new TenantAgnosticProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId), CreateMaterializedWorkflow("workflow-2", "id-2", Tenant.AgnosticTenantId));
        var store = CreateStore();
        store.FindManyAsync(Arg.Is<WorkflowDefinitionFilter>(x => x.Ids != null), Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException());

        var result = await CreatePopulatorWithTenant("tenant-1", provider, new(), store).PopulateStoreAsync();

        Assert.Equal(2, result.Count());
        Assert.Equal(2, store.Rows.Count);
    }

    [Fact(DisplayName = "PopulateStoreAsync builds a tenant-agnostic provider once when tenants are populated concurrently")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderConcurrentTenants_BuiltOnce()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var provider = new BlockingProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));

        var first = CreatePopulatorWithTenant("tenant-1", provider, cache, CreateStore()).PopulateStoreAsync();
        await provider.Started.Task;
        var second = CreatePopulatorWithTenant("tenant-2", provider, cache, CreateStore()).PopulateStoreAsync();
        provider.Release.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(1, provider.CallCount);
    }

    [Fact(DisplayName = "PopulateStoreAsync does not remember a build made while triggers are not indexed")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderBuiltWithoutIndexingTriggers_BuiltAgain()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var provider = new TenantAgnosticProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));

        await CreatePopulatorWithTenant("tenant-1", provider, cache, CreateStore()).PopulateStoreAsync(false);
        await CreatePopulatorWithTenant("tenant-2", provider, cache, CreateStore()).PopulateStoreAsync(true);
        await CreatePopulatorWithTenant("tenant-3", provider, cache, CreateStore()).PopulateStoreAsync(true);

        Assert.Equal(2, provider.CallCount);
    }

    [Fact(DisplayName = "PopulateStoreAsync neither saves nor indexes triggers for an identical stored definition when triggers are not indexed")]
    public async Task PopulateStoreAsync_TenantAgnosticProviderIdenticalRowsStoredWithoutIndexingTriggers_DoesNotSaveOrIndex()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var store = CreateStore();
        var provider = new TenantAgnosticProviderStub(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId));
        await CreatePopulatorWithTenant("tenant-1", provider, cache, store).PopulateStoreAsync();
        store.ClearReceivedCalls();
        var triggerIndexer = Substitute.For<ITriggerIndexer>();

        var result = await CreatePopulatorWithTenant("tenant-2", provider, cache, store, triggerIndexer).PopulateStoreAsync(false);

        Assert.Equal("id-1", Assert.Single(result).Id);
        await store.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
        await triggerIndexer.DidNotReceive().IndexTriggersAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A cancelled wait for another build does not prevent the next call from building and caching")]
    public async Task GetOrBuildAsync_WaitCancelledWhileAnotherBuildRuns_NextCallBuildsAndCaches()
    {
        var cache = new TenantAgnosticWorkflowsCache();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyList<ImportedWorkflow> workflows = [new(CreateMaterializedWorkflow("workflow-1", "id-1", Tenant.AgnosticTenantId), true, true, true, null, null, null)];
        var builds = 0;

        Task<IReadOnlyList<ImportedWorkflow>> Build()
        {
            builds++;
            return Task.FromResult(workflows);
        }

        var holder = cache.GetOrBuildAsync(typeof(object), async () =>
        {
            started.SetResult();
            await release.Task;
            return workflows;
        }, true);
        await started.Task;
        using var cancellation = new CancellationTokenSource();
        var cancelled = cache.GetOrBuildAsync(typeof(string), Build, true, cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        release.SetResult();
        await holder;

        await cache.GetOrBuildAsync(typeof(string), Build, true);
        await cache.GetOrBuildAsync(typeof(string), Build, true);

        Assert.Equal(1, builds);
    }

    private static IClrWorkflowDefinitionTenantRestamper? GetClrTenantRestamper(DefaultWorkflowDefinitionStorePopulator populator)
    {
        return (IClrWorkflowDefinitionTenantRestamper?)typeof(DefaultWorkflowDefinitionStorePopulator)
            .GetField("_clrTenantRestamper", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(populator);
    }

    private MaterializedWorkflow CreateMaterializedWorkflow(string definitionId, string id, string? tenantId, int version = 1, bool isPublished = true)
    {
        return new(new()
        {
            Identity = new(definitionId, version, id, tenantId),
            Publication = new(true, isPublished)
        }, "Test", "TestProvider");
    }

    private DefaultWorkflowDefinitionStorePopulator CreatePopulatorWithTenant(string? tenantId, params MaterializedWorkflow[] workflows) =>
        CreatePopulatorWithTenant(tenantId, Substitute.For<ILogger<DefaultWorkflowDefinitionStorePopulator>>(), restamper: null, workflows);

    private DefaultWorkflowDefinitionStorePopulator CreatePopulatorWithTenant(
        string? tenantId,
        ILogger<DefaultWorkflowDefinitionStorePopulator> logger,
        params MaterializedWorkflow[] workflows) =>
        CreatePopulatorWithTenant(tenantId, logger, restamper: null, workflows);

    private DefaultWorkflowDefinitionStorePopulator CreatePopulatorWithTenant(
        string? tenantId,
        IClrWorkflowDefinitionTenantRestamper restamper,
        params MaterializedWorkflow[] workflows) =>
        CreatePopulatorWithTenant(tenantId, Substitute.For<ILogger<DefaultWorkflowDefinitionStorePopulator>>(), restamper, workflows);

    private DefaultWorkflowDefinitionStorePopulator CreatePopulatorWithTenant(
        string? tenantId,
        ILogger<DefaultWorkflowDefinitionStorePopulator> logger,
        IClrWorkflowDefinitionTenantRestamper? restamper,
        params MaterializedWorkflow[] workflows) =>
        CreatePopulatorWithTenant(tenantId, logger, restamper, triggerIndexer: null, workflows);

    private DefaultWorkflowDefinitionStorePopulator CreatePopulatorWithTenant(
        string? tenantId,
        ILogger<DefaultWorkflowDefinitionStorePopulator> logger,
        IClrWorkflowDefinitionTenantRestamper? restamper,
        ITriggerIndexer? triggerIndexer,
        params MaterializedWorkflow[] workflows)
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
            triggerIndexer ?? Substitute.For<ITriggerIndexer>(),
            _storeMock,
            Substitute.For<IActivitySerializer>(),
            Substitute.For<IPayloadSerializer>(),
            Substitute.For<ISystemClock>(),
            Substitute.For<IIdentityGraphService>(),
            tenantAccessor,
            logger,
            restamper);
    }

    private static MaterializedWorkflow CreateRichMaterializedWorkflow()
    {
        return new(new()
        {
            Identity = new("workflow-1", 1, "id-1", Tenant.AgnosticTenantId),
            Publication = new(true, true),
            WorkflowMetadata = new("Name", "Description", default, new(1, 2)),
            IsReadonly = true,
            Options = new() { ActivityCategory = "Category" },
            Variables = new List<Variable> { new("Variable") },
            Inputs = new List<InputDefinition> { new() { Name = "Input" } },
            Outputs = new List<OutputDefinition> { new() { Name = "Output" } },
            Outcomes = new List<string> { "Outcome" },
            CustomProperties = new Dictionary<string, object> { ["Key"] = "Value" }
        }, "Test", "TestProvider", "MaterializerContext");
    }

    // A serializer whose output reflects the compared content, unlike a bare substitute that returns an empty string.
    private static IPayloadSerializer CreateDescribingPayloadSerializer()
    {
        var serializer = Substitute.For<IPayloadSerializer>();
        serializer.Serialize(Arg.Any<object>()).Returns(call =>
        {
            var payload = call.Arg<object>();

            if (payload is string text)
            {
                return text;
            }

            return string.Join("|", payload.GetType().GetProperties().Select(x => x.GetValue(payload) switch
            {
                WorkflowOptions options => options.ActivityCategory,
                string value => value,
                System.Collections.IEnumerable items => items.Cast<object>().Count().ToString(),
                var value => value?.ToString()
            }));
        });
        return serializer;
    }

    private static TProvider CreateProvider<TProvider>(params MaterializedWorkflow[] workflows) where TProvider : class, IWorkflowsProvider
    {
        var provider = Substitute.For<TProvider>();
        provider.Name.Returns("TestProvider");
        provider.GetWorkflowsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IEnumerable<MaterializedWorkflow>>(workflows));
        return provider;
    }

    private static bool Matches(WorkflowDefinitionFilter filter, WorkflowDefinition row)
    {
        if (filter.Ids != null && !filter.Ids.Contains(row.Id))
        {
            return false;
        }

        if (filter.DefinitionId != null && filter.DefinitionId != row.DefinitionId)
        {
            return false;
        }

        var versionOptions = filter.VersionOptions;

        if (versionOptions == null)
        {
            return true;
        }

        if (versionOptions.Value.IsLatest)
        {
            return row.IsLatest;
        }

        if (versionOptions.Value.IsPublished)
        {
            return row.IsPublished;
        }

        return versionOptions.Value.Version == 0 || row.Version == versionOptions.Value.Version;
    }

    private static IRecordingWorkflowDefinitionStore CreateStore()
    {
        var rows = new List<WorkflowDefinition>();
        var store = Substitute.For<IRecordingWorkflowDefinitionStore>();
        store.Rows.Returns(rows);
        store.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(call => rows.Where(x => Matches(call.Arg<WorkflowDefinitionFilter>(), x)).ToList());
        store.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(call => rows.FirstOrDefault(x => Matches(call.Arg<WorkflowDefinitionFilter>(), x)));
        store.SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                foreach (var definition in call.Arg<IEnumerable<WorkflowDefinition>>().Where(x => !rows.Contains(x)))
                {
                    rows.Add(definition);
                }

                return Task.CompletedTask;
            });
        return store;
    }

    private static DefaultWorkflowDefinitionStorePopulator CreatePublicConstructorPopulatorWithTenant(string tenantId, IWorkflowsProvider provider, IWorkflowDefinitionStore store)
    {
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.TenantId.Returns(tenantId);

        return new(
            () => new List<IWorkflowsProvider> { provider },
            Substitute.For<ITriggerIndexer>(),
            store,
            Substitute.For<IActivitySerializer>(),
            Substitute.For<IPayloadSerializer>(),
            Substitute.For<ISystemClock>(),
            Substitute.For<IIdentityGraphService>(),
            tenantAccessor,
            Substitute.For<ILogger<DefaultWorkflowDefinitionStorePopulator>>());
    }

    private static MaterializedWorkflow CreateClrMaterializedWorkflow()
    {
        return new(new()
        {
            Identity = new("workflow-1", 1, "id-1", Tenant.AgnosticTenantId),
            Publication = new(true, true)
        }, "CLR", "ClrWorkflowMaterializer");
    }

    private DefaultWorkflowDefinitionStorePopulator CreatePopulatorWithTenant(string? tenantId, IWorkflowsProvider provider, TenantAgnosticWorkflowsCache cache, IWorkflowDefinitionStore? store = null, ITriggerIndexer? triggerIndexer = null, IPayloadSerializer? payloadSerializer = null, IActivitySerializer? activitySerializer = null, IClrWorkflowDefinitionTenantRestamper? restamper = null)
    {
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.Tenant.Returns(tenantId != null ? new Tenant { Id = tenantId } : null);
        tenantAccessor.TenantId.Returns(tenantId.NormalizeTenantId());

        return new(
            () => new List<IWorkflowsProvider> { provider },
            triggerIndexer ?? Substitute.For<ITriggerIndexer>(),
            store ?? _storeMock,
            activitySerializer ?? Substitute.For<IActivitySerializer>(),
            payloadSerializer ?? Substitute.For<IPayloadSerializer>(),
            Substitute.For<ISystemClock>(),
            Substitute.For<IIdentityGraphService>(),
            tenantAccessor,
            cache,
            Substitute.For<ILogger<DefaultWorkflowDefinitionStorePopulator>>(),
            restamper);
    }

    private sealed class TenantAgnosticProviderStub(params MaterializedWorkflow[] workflows) : ITenantAgnosticWorkflowsProvider
    {
        public int CallCount { get; private set; }

        public Func<IEnumerable<MaterializedWorkflow>> GetWorkflows { get; init; } = () => workflows;

        public string Name => "TestProvider";

        public ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return new(GetWorkflows());
        }
    }

    private sealed class BlockingProviderStub(params MaterializedWorkflow[] workflows) : ITenantAgnosticWorkflowsProvider
    {
        private int _callCount;

        public int CallCount => _callCount;

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Name => "TestProvider";

        public async ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            Started.TrySetResult();
            await Release.Task;
            return workflows;
        }
    }

    public interface IRecordingWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        List<WorkflowDefinition> Rows { get; }
    }

    private sealed class CapturingLogger : ILogger<DefaultWorkflowDefinitionStorePopulator>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
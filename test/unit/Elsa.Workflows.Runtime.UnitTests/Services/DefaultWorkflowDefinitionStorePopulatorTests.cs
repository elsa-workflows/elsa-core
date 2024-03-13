using Elsa.Common.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DefaultWorkflowDefinitionStorePopulatorTests
{
    private readonly IWorkflowDefinitionStore _storeMock;
    private readonly DefaultWorkflowDefinitionStorePopulator _populator;
    
    public DefaultWorkflowDefinitionStorePopulatorTests()
    {
        _storeMock = Substitute.For<IWorkflowDefinitionStore>();
        _populator = new DefaultWorkflowDefinitionStorePopulator(() => new List<IWorkflowProvider>(),
            Substitute.For<ITriggerIndexer>(),
            _storeMock,
            Substitute.For<IActivitySerializer>(),
            Substitute.For<IPayloadSerializer>(),
            Substitute.For<ISystemClock>(),
            Substitute.For<IIdentityGraphService>(),
            Substitute.For<ILogger<DefaultWorkflowDefinitionStorePopulator>>());
    }
    
    [Fact]
    public async Task AddOrUpdateCoreAsync_NewWorkflowDefinition_AddsWorkflowDefinition()
    {        
        var workflow = new MaterializedWorkflow(new Workflow{Identity = new WorkflowIdentity("a", 1, "1")}, "Test", "Test");
        
        await _populator.AddAsync(workflow);

        await _storeMock.Received(1).SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
        await _storeMock.Received(1).SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l => 
            l.Any(wd => wd.Id == "1" 
                        && wd.DefinitionId == "a"
                        && wd.Version == 1)), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task AddOrUpdateCoreAsync_ExistingWorkflowDefinition_KeepsExistingWorkflow()
    {
        _storeMock.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowDefinition>
            {
                new()
                {
                    Id = "1",
                    DefinitionId = "a",
                    Version = 1,
                    Inputs = new List<InputDefinition>{new(){Name = "InputA"}}
                }
            });
        
        var workflow = new MaterializedWorkflow(new Workflow
        {
            Identity = new WorkflowIdentity("a", 1, "1"),
            Inputs = new List<InputDefinition>{new (){Name = "A"}}
        }, "Test", "Test");
        
        await _populator.AddAsync(workflow);

        await _storeMock.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task AddOrUpdateCoreAsync_NewWorkflowDefinitionVersion_AddsWorkflowDefinition()
    {
        var workflow = new MaterializedWorkflow(new Workflow{Identity = new WorkflowIdentity("a", 2, "2")}, "Test", "Test");
        
        await _populator.AddAsync(workflow);

        await _storeMock.Received(1).SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l => 
            l.Any(wd => wd.Id == "2" 
                        && wd.DefinitionId == "a"
                        && wd.Version == 2)), Arg.Any<CancellationToken>());
        
    }
    
    [Fact]
    public async Task AddOrUpdateCoreAsync_NewWorkflowDefinitionVersionAsLatestAndPublished_UpdatesOlderDefinitions()
    {
        _storeMock.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowDefinition>
            {
                new()
                {
                    Id = "1",
                    DefinitionId = "a",
                    Version = 1,
                    IsLatest = true,
                    IsPublished = true
                }
            });
        
        var workflow = new MaterializedWorkflow(new Workflow{Identity = new WorkflowIdentity("a", 2, "2")}, "Test", "Test");
        
        await _populator.AddAsync(workflow);

        await _storeMock.Received(1).SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l => 
            l.Any(wd => wd.Id == "1"
                && wd.IsLatest == false
                && wd.IsPublished == false)), Arg.Any<CancellationToken>());
        
        await _storeMock.Received(1).SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l => 
            l.Any(wd => wd.Id == "2" 
                        && wd.DefinitionId == "a"
                        && wd.Version == 2
                        && wd.IsLatest == true
                        && wd.IsPublished == true)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrUpdateCoreAsync_NewWorkflowDefinitionVersionWithSameId_IsIgnored()
    {
        _storeMock.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowDefinition>
            {
                new()
                {
                    Id = "1",
                    DefinitionId = "a",
                    Version = 1,
                }
            });
        
        var workflow = new MaterializedWorkflow(new Workflow{Identity = new WorkflowIdentity("a", 2, "1")}, "Test", "Test");
        
        await _populator.AddAsync(workflow);
        await _storeMock.DidNotReceive().SaveManyAsync(Arg.Any<IEnumerable<WorkflowDefinition>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrUpdateCoreAsync_OlderVersionAsLatest_ExistingVersionShouldRemainLatest()
    {
        _storeMock.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowDefinition>
            {
                new()
                {
                    Id = "2",
                    DefinitionId = "a",
                    Version = 2,
                    IsLatest = true
                }
            });
        
        var workflow = new MaterializedWorkflow(new Workflow{Identity = new WorkflowIdentity("a", 1, "1"), Publication = new WorkflowPublication(true, true)}, "Test", "Test");
        
        await _populator.AddAsync(workflow);
        await _storeMock.DidNotReceive().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l => 
            l.Any(wd => wd.Id == "2")), Arg.Any<CancellationToken>());
        await _storeMock.Received().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l => 
            l.Any(wd => wd.Id == "1" && wd.IsLatest == false)), Arg.Any<CancellationToken>());
    }

    [Fact]
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
        
        var workflow = new MaterializedWorkflow(new Workflow{Identity = new WorkflowIdentity("a", 1, "1"), Publication = new WorkflowPublication(true, true)}, "Test", "Test");
        
        await _populator.AddAsync(workflow);
        await _storeMock.DidNotReceive().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l => 
            l.Any(wd => wd.Id == "2")), Arg.Any<CancellationToken>());
        await _storeMock.Received().SaveManyAsync(Arg.Is<IEnumerable<WorkflowDefinition>>(l => 
            l.Any(wd => wd.Id == "1" && wd.IsPublished == false)), Arg.Any<CancellationToken>());
    }
}
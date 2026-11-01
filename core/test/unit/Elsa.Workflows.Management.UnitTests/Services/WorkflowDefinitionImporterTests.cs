using Elsa.Common.Models;
using Elsa.Common.Serialization;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowDefinitionImporterTests
{
    private const string GeneratedId = "generated-id";
    private const string ExportedId = "exported-id";
    private const string DefinitionId = "def-1";

    private readonly IWorkflowDefinitionPublisher _publisher = Substitute.For<IWorkflowDefinitionPublisher>();
    private readonly IWorkflowDefinitionStore _store = Substitute.For<IWorkflowDefinitionStore>();

    public WorkflowDefinitionImporterTests()
    {
        _publisher.GetDraftAsync(Arg.Any<string>(), Arg.Any<VersionOptions>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);
        _publisher.NewAsync(Arg.Any<IActivity?>(), Arg.Any<CancellationToken>()).Returns(_ => new WorkflowDefinition { Id = GeneratedId });
        _publisher.SaveDraftAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<WorkflowDefinition>());
    }

    [Fact(DisplayName = "A new import keeps the exported version id while no tenant uses it")]
    public async Task ImportAsync_NewDefinition_KeepsFreeExportedId()
    {
        _store.AnyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateImporter().ImportAsync(CreateRequest());

        Assert.Equal(ExportedId, result.WorkflowDefinition.Id);
        Assert.Equal(DefinitionId, result.WorkflowDefinition.DefinitionId);
    }

    [Fact(DisplayName = "A new import into a second tenant keeps the definition id but gets a fresh version id")]
    public async Task ImportAsync_NewDefinition_ReplacesExportedIdTakenByAnyTenant()
    {
        _store.AnyAsync(Arg.Is<WorkflowDefinitionFilter>(x => x.Id == ExportedId && x.TenantAgnostic), Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateImporter().ImportAsync(CreateRequest());

        Assert.Equal(GeneratedId, result.WorkflowDefinition.Id);
        Assert.Equal(DefinitionId, result.WorkflowDefinition.DefinitionId);
    }

    [Fact(DisplayName = "Re-importing into the same tenant updates the existing draft without consulting the store")]
    public async Task ImportAsync_ExistingDraft_UpdatesDraftAndKeepsItsIds()
    {
        var existingDraft = new WorkflowDefinition { Id = "existing-version-id", DefinitionId = DefinitionId, Version = 1, Name = "Before" };
        _publisher.GetDraftAsync(DefinitionId, VersionOptions.Latest, Arg.Any<CancellationToken>()).Returns(existingDraft);

        var result = await CreateImporter().ImportAsync(CreateRequest());

        Assert.Same(existingDraft, result.WorkflowDefinition);
        Assert.Equal("existing-version-id", result.WorkflowDefinition.Id);
        Assert.Equal("Imported", result.WorkflowDefinition.Name);
        await _publisher.DidNotReceive().NewAsync(Arg.Any<IActivity?>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().AnyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>());
    }

    private static SaveWorkflowDefinitionRequest CreateRequest() => new()
    {
        Model = new WorkflowDefinitionModel { Id = ExportedId, DefinitionId = DefinitionId, Name = "Imported" },
        Publish = false
    };

    private WorkflowDefinitionImporter CreateImporter()
    {
        var serializer = Substitute.For<IActivitySerializer>();
        serializer.Serialize(Arg.Any<IActivity>()).Returns("{}");
        var variableMapper = new VariableDefinitionMapper(
            SerializationTypeRegistry.CreateDefault(),
            Substitute.For<IServiceScopeFactory>(),
            NullLogger<VariableDefinitionMapper>.Instance);
        return new WorkflowDefinitionImporter(serializer, _publisher, variableMapper, _store);
    }
}

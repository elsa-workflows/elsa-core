using Elsa.Common.Models;
using Elsa.Common.Serialization;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowDefinitionImporterOriginalSourceTests
{
    private readonly IActivitySerializer _serializer = Substitute.For<IActivitySerializer>();
    private readonly IWorkflowDefinitionPublisher _publisher = Substitute.For<IWorkflowDefinitionPublisher>();
    private readonly WorkflowDefinitionImporter _importer;
    private readonly WorkflowDefinitionMapper _mapper;

    public WorkflowDefinitionImporterOriginalSourceTests()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var variableDefinitionMapper = new VariableDefinitionMapper(
            SerializationTypeRegistry.CreateDefault(),
            scopeFactory,
            NullLogger<VariableDefinitionMapper>.Instance);
        _importer = new(_serializer, _publisher, variableDefinitionMapper);
        _mapper = new(_serializer, Substitute.For<IWorkflowDefinitionService>(), variableDefinitionMapper);
    }

    [Fact]
    public async Task ImportAsync_WhenFileImportedDefinitionIsEdited_ClearsOriginalSourceAndMapUsesStringData()
    {
        var fileImportedDraft = new WorkflowDefinition
        {
            Id = "version-1",
            DefinitionId = "def-1",
            Version = 1,
            Name = "FromFile",
            StringData = "FILE_ROOT",
            OriginalSource = "FILE_JSON",
            MaterializerName = JsonWorkflowMaterializer.MaterializerName
        };
        var editedRoot = new Sequence { Id = "edited-root" };
        var mappedRoot = new WriteLine("from-studio");

        _publisher.GetDraftAsync("def-1", VersionOptions.Latest, Arg.Any<CancellationToken>())
            .Returns(fileImportedDraft);
        _publisher.SaveDraftAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<WorkflowDefinition>());
        _serializer.Serialize(Arg.Any<IActivity>()).Returns("EDITED_JSON");
        _serializer.Deserialize("EDITED_JSON").Returns(mappedRoot);
        _serializer.Deserialize<WorkflowDefinitionModel>("FILE_JSON")
            .Returns(new WorkflowDefinitionModel
            {
                Name = "FromFile",
                Root = new WriteLine("from-file")
            });

        var result = await _importer.ImportAsync(new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                DefinitionId = "def-1",
                Name = "Edited",
                Root = editedRoot
            },
            Publish = false
        });

        Assert.True(result.Succeeded);
        Assert.Null(result.WorkflowDefinition.OriginalSource);
        Assert.Equal("EDITED_JSON", result.WorkflowDefinition.StringData);
        Assert.Equal("Edited", result.WorkflowDefinition.Name);
        Assert.Equal(JsonWorkflowMaterializer.MaterializerName, result.WorkflowDefinition.MaterializerName);

        var workflow = _mapper.Map(result.WorkflowDefinition);

        Assert.Same(mappedRoot, workflow.Root);
        Assert.Equal("Edited", workflow.WorkflowMetadata.Name);
        _serializer.DidNotReceive().Deserialize<WorkflowDefinitionModel>("FILE_JSON");
    }
}

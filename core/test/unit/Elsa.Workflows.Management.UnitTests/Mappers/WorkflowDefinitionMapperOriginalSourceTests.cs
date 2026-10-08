using Elsa.Common.Serialization;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Mappers;

public class WorkflowDefinitionMapperOriginalSourceTests
{
    private readonly IActivitySerializer _activitySerializer = Substitute.For<IActivitySerializer>();
    private readonly WorkflowDefinitionMapper _mapper;

    public WorkflowDefinitionMapperOriginalSourceTests()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var variableDefinitionMapper = new VariableDefinitionMapper(
            SerializationTypeRegistry.CreateDefault(),
            scopeFactory,
            NullLogger<VariableDefinitionMapper>.Instance);
        var workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
        _mapper = new(_activitySerializer, workflowDefinitionService, variableDefinitionMapper);
    }

    [Fact]
    public void Map_WhenStringDataAndOriginalSourceBothPresent_UsesEditedStringData()
    {
        var fileRoot = new WriteLine("from-file");
        var editedRoot = new WriteLine("from-studio");
        _activitySerializer.Deserialize<WorkflowDefinitionModel>("FILE_JSON")
            .Returns(new WorkflowDefinitionModel
            {
                Name = "FromFile",
                Root = fileRoot
            });
        _activitySerializer.Deserialize("EDITED_JSON").Returns(editedRoot);

        var definition = CreateDefinition(
            name: "Edited",
            stringData: "EDITED_JSON",
            originalSource: "FILE_JSON");

        var workflow = _mapper.Map(definition);

        Assert.Same(editedRoot, workflow.Root);
        Assert.Equal("Edited", workflow.WorkflowMetadata.Name);
        _activitySerializer.DidNotReceive().Deserialize<WorkflowDefinitionModel>("FILE_JSON");
    }

    [Fact]
    public void Map_WhenOnlyOriginalSourceIsPresent_UsesOriginalSource()
    {
        var sourceRoot = new WriteLine("from-elsascript-equivalent");
        _activitySerializer.Deserialize<WorkflowDefinitionModel>("SOURCE")
            .Returns(new WorkflowDefinitionModel
            {
                Name = "FromSource",
                Root = sourceRoot
            });

        var definition = CreateDefinition(
            name: "EntityName",
            stringData: null,
            originalSource: "SOURCE");

        var workflow = _mapper.Map(definition);

        Assert.Same(sourceRoot, workflow.Root);
        Assert.Equal("FromSource", workflow.WorkflowMetadata.Name);
        _activitySerializer.DidNotReceive().Deserialize(Arg.Any<string>());
    }

    private static WorkflowDefinition CreateDefinition(string name, string? stringData, string? originalSource)
    {
        return new()
        {
            Id = "version-1",
            DefinitionId = "def-1",
            Version = 1,
            Name = name,
            StringData = stringData,
            OriginalSource = originalSource,
            MaterializerName = JsonWorkflowMaterializer.MaterializerName
        };
    }
}

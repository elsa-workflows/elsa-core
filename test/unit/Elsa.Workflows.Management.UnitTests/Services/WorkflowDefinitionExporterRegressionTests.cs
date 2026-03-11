using System.IO.Compression;
using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowDefinitionExporterRegressionTests
{
    private readonly IWorkflowDefinitionStore _store = Substitute.For<IWorkflowDefinitionStore>();
    private readonly IApiSerializer _serializer = Substitute.For<IApiSerializer>();
    private readonly IWorkflowDefinitionService _workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
    private readonly IWorkflowReferenceGraphBuilder _workflowReferenceGraphBuilder = Substitute.For<IWorkflowReferenceGraphBuilder>();

    [Fact]
    public async Task ExportManyAsync_WithSlashInWorkflowName_CreatesFlatZipEntry()
    {
        var definition = new WorkflowDefinition
        {
            Id = "slash-name-workflow-v1",
            DefinitionId = "slash-name-workflow",
            Name = "folder/child",
            CreatedAt = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Version = 1,
            IsLatest = true,
            IsPublished = true,
            MaterializerName = "Json"
        };

        _serializer.GetOptions().Returns(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _store.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IEnumerable<WorkflowDefinition>>([definition]));
        _workflowDefinitionService.MaterializeWorkflowAsync(definition, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateWorkflowGraph()));

        var sut = CreateExporter();
        var result = await sut.ExportManyAsync([definition.Id]);

        Assert.NotNull(result);
        Assert.Equal("workflow-definitions.zip", result.FileName);

        using var zipStream = new MemoryStream(result.Data);
        await using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var entry = Assert.Single(zipArchive.Entries);

        Assert.Equal(entry.Name, entry.FullName);
        Assert.DoesNotContain('/', entry.FullName);
        Assert.DoesNotContain('\\', entry.FullName);
        Assert.Equal("workflow-definition-folder-child-slash-name-workflow.json", entry.FullName);
    }

    private WorkflowDefinitionExporter CreateExporter()
    {
        var activitySerializer = Substitute.For<IActivitySerializer>();
        var wellKnownTypeRegistry = Substitute.For<IWellKnownTypeRegistry>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var variableDefinitionMapper = new VariableDefinitionMapper(wellKnownTypeRegistry, scopeFactory, NullLogger<VariableDefinitionMapper>.Instance);
        var workflowDefinitionMapper = new WorkflowDefinitionMapper(activitySerializer, _workflowDefinitionService, variableDefinitionMapper);

        return new(_store, _serializer, workflowDefinitionMapper, _workflowReferenceGraphBuilder);
    }

    private static WorkflowGraph CreateWorkflowGraph()
    {
        var root = new Sequence { Id = "root" };
        var rootNode = new ActivityNode(root, "Root");
        var workflow = new Workflow(root);

        return new(workflow, rootNode, [rootNode]);
    }
}
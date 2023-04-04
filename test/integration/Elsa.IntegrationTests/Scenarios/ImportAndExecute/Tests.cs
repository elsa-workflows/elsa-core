using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.ImportAndExecute;

public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IActivityRegistry _activityRegistry;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
        _workflowDefinitionPublisher = services.GetRequiredService<IWorkflowDefinitionPublisher>();
        _serializerOptionsProvider = services.GetRequiredService<SerializerOptionsProvider>();
        _workflowRuntime = services.GetRequiredService<IWorkflowRuntime>();
        _activityRegistry = services.GetRequiredService<IActivityRegistry>();
        _workflowDefinitionService = services.GetRequiredService<IWorkflowDefinitionService>();
    }

    [Fact(DisplayName = "Workflow imported from file should execute successfully.")]
    public async Task Test1()
    {
        // Prereq
        await _activityRegistry.RegisterAsync(typeof(Workflow));
        await _activityRegistry.RegisterAsync(typeof(Flowchart));
        await _activityRegistry.RegisterAsync(typeof(WriteLine));

        // Import
        string fileName = @"Scenarios/ImportAndExecute/workflow.json";
        using FileStream openStream = File.OpenRead(fileName);

        var options = _serializerOptionsProvider.CreatePersistenceOptions();
        var workflowDefinition = await JsonSerializer.DeserializeAsync<ExportedWorkflowDefinition>(openStream, options);
        workflowDefinition!.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
        workflowDefinition.StringData = workflowDefinition.Root.RootElement.ToString();

        // Publish
        var publishedWorkflowDefinition = await _workflowDefinitionPublisher.PublishAsync(workflowDefinition);

        var materializedWorkflow =
            await _workflowDefinitionService.MaterializeWorkflowAsync(publishedWorkflowDefinition);

        // Execute
        var startWorkflowOptions = new StartWorkflowRuntimeOptions(null, new Dictionary<string, object>(), VersionOptions.Published);
        var result = _workflowRuntime.StartWorkflowAsync(workflowDefinition.DefinitionId, startWorkflowOptions);

        // Assert
        var lines = _capturingTextWriter.Lines.ToList();

        Assert.Equal(new[] { "Dummy text" }, lines);
    }

    class ExportedWorkflowDefinition : WorkflowDefinition
    {
        public JsonDocument Root { get; set; }
    }
}

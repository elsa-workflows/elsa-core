using Elsa.Common.Models;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Contracts;
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
    private readonly IWorkflowDefinitionImporter _workflowDefinitionImporter;
    private readonly IActivitySerializer _serializer;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IActivityRegistry _activityRegistry;
    private readonly IExpressionSyntaxRegistry _expressionSyntaxRegistry;
    private readonly IEnumerable<IExpressionSyntaxProvider> _expressionSyntaxProviders;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa => elsa.UseWorkflowsApi())
            .Build();
        _serializer = services.GetRequiredService<IActivitySerializer>();
        _workflowRuntime = services.GetRequiredService<IWorkflowRuntime>();
        _activityRegistry = services.GetRequiredService<IActivityRegistry>();
        _expressionSyntaxRegistry = services.GetRequiredService<IExpressionSyntaxRegistry>();
        _expressionSyntaxProviders = services.GetServices<IExpressionSyntaxProvider>();
        _workflowDefinitionImporter = services.GetRequiredService<IWorkflowDefinitionImporter>();
    }

    [Fact(DisplayName = "Workflow imported from file should execute successfully.")]
    public async Task Test1()
    {
        // Register activities.
        await _activityRegistry.RegisterAsync(typeof(Workflow));
        await _activityRegistry.RegisterAsync(typeof(Flowchart));
        await _activityRegistry.RegisterAsync(typeof(WriteLine));

        // Register expression syntaxes.
        foreach (var syntaxProvider in _expressionSyntaxProviders)
        {
            var syntaxes = await syntaxProvider.GetDescriptorsAsync();
            _expressionSyntaxRegistry.AddMany(syntaxes);
        }

        // Import and publish workflow.
        var fileName = @"Scenarios/ImportAndExecute/workflow.json";
        var json = await File.ReadAllTextAsync(fileName);
        var workflowDefinitionRequest = _serializer.Deserialize<SaveWorkflowDefinitionRequest>(json);

        workflowDefinitionRequest.Publish = true;
        var workflowDefinition = await _workflowDefinitionImporter.ImportAsync(workflowDefinitionRequest);

        // Execute.
        var startWorkflowOptions = new StartWorkflowRuntimeOptions(null, new Dictionary<string, object>(), VersionOptions.Published);
        await _workflowRuntime.StartWorkflowAsync(workflowDefinition.DefinitionId, startWorkflowOptions);

        // Assert.
        var lines = _capturingTextWriter.Lines.ToList();

        Assert.Equal(new[] { "Dummy Text" }, lines);
    }
}

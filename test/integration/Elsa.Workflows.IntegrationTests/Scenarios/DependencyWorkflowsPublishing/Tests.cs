using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DependencyWorkflowsPublishing;

/// <summary>
/// Contains tests for the "DependencyWorkflowsPublishing" scenario.
/// </summary>
public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IActivityVisitor _activityVisitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tests"/> class.
    /// </summary>
    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .WithWorkflowsFromDirectory("Scenarios", "DependencyWorkflowsPublishing", "Workflows")
            .Build();

        _workflowDefinitionPublisher = _services.GetRequiredService<IWorkflowDefinitionPublisher>();
        _activitySerializer = _services.GetRequiredService<IActivitySerializer>();
        _activityVisitor = _services.GetRequiredService<IActivityVisitor>();
    }

    /// <summary>
    /// When a dependency workflow is published, all consuming workflows are updated to point to the new version of the dependency.
    /// </summary>
    [Fact(DisplayName = "When a dependency workflow is published, all consuming workflows are updated to point to the new version of the dependency.")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        var childDefinitionV1 = await _services.GetWorkflowDefinitionAsync("child", VersionOptions.Latest);
        var parentDefinition = await _services.GetWorkflowDefinitionAsync("parent", VersionOptions.Latest);
        var childActivityV1 = await GetChildActivityAsync(parentDefinition);

        // Assert initial state.
        Assert.Equal(1, childDefinitionV1.Version);
        Assert.False(parentDefinition.IsPublished);
        Assert.Equal(1, childActivityV1.Version);

        // Create a new draft for the child workflow and publish it.
        var childDefinitionV2 = (await _workflowDefinitionPublisher.GetDraftAsync(childDefinitionV1.DefinitionId, VersionOptions.Published))!;
        await _workflowDefinitionPublisher.PublishAsync(childDefinitionV2);

        // Assert that the parent workflow now points to the new version of the child workflow.
        parentDefinition = await _services.GetWorkflowDefinitionAsync("parent", VersionOptions.Latest);
        var childActivityV2 = await GetChildActivityAsync(parentDefinition);
        Assert.Equal(2, childActivityV2.Version);
    }

    private async Task<WorkflowDefinitionActivity> GetChildActivityAsync(WorkflowDefinition parent)
    {
        var root = _activitySerializer.Deserialize(parent.StringData!);
        var graph = await _activityVisitor.VisitAsync(root);
        var flattenedList = graph.Flatten().ToList();
        return (WorkflowDefinitionActivity)flattenedList.Single(x => x.Activity is WorkflowDefinitionActivity { WorkflowDefinitionId: "child" }).Activity;
    }
}
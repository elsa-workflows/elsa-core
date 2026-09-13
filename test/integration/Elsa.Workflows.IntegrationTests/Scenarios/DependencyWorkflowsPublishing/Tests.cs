using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DependencyWorkflowsPublishing;

/// <summary>
/// Contains tests for the "DependencyWorkflowsPublishing" scenario.
/// </summary>
public class Tests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IActivityVisitor _activityVisitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tests"/> class.
    /// </summary>
    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
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
    [Test]
    [DisplayName("When a dependency workflow is published, all consuming workflows are updated to point to the new version of the dependency.")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        var childDefinitionV1 = await _services.GetWorkflowDefinitionAsync("child", VersionOptions.Latest);
        var parentDefinition = await _services.GetWorkflowDefinitionAsync("parent", VersionOptions.Latest);
        var childActivityV1 = await GetChildActivityAsync(parentDefinition);

        // Assert initial state.
        await Assert.That(childDefinitionV1.Version).IsEqualTo(1);
        await Assert.That(parentDefinition.IsPublished).IsFalse();
        await Assert.That(childActivityV1.Version).IsEqualTo(1);

        // Create a new draft for the child workflow and publish it.
        var childDefinitionV2 = (await _workflowDefinitionPublisher.GetDraftAsync(childDefinitionV1.DefinitionId, VersionOptions.Published))!;
        await _workflowDefinitionPublisher.PublishAsync(childDefinitionV2);

        // Assert that the parent workflow now points to the new version of the child workflow.
        parentDefinition = await _services.GetWorkflowDefinitionAsync("parent", VersionOptions.Latest);
        var childActivityV2 = await GetChildActivityAsync(parentDefinition);
        await Assert.That(childActivityV2.Version).IsEqualTo(2);
    }

    private async Task<WorkflowDefinitionActivity> GetChildActivityAsync(WorkflowDefinition parent)
    {
        var root = _activitySerializer.Deserialize(parent.StringData!);
        var graph = await _activityVisitor.VisitAsync(root);
        var flattenedList = graph.Flatten().ToList();
        return (WorkflowDefinitionActivity)flattenedList.Single(x => x.Activity is WorkflowDefinitionActivity { WorkflowDefinitionId: "child" }).Activity;
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}

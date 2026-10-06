using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Composition;

public class DispatchWorkflowTests
{
    private const string DefaultWorkflowDefinitionId = "test-workflow-def";
    private const string DefaultGeneratedId = "generated-child-id";

    [Fact]
    public async Task Should_Set_Result_To_Dispatched_Workflow_Instance_Id_When_Not_Waiting()
    {
        var dispatchWorkflow = new DispatchWorkflow
        {
            WorkflowDefinitionId = new(DefaultWorkflowDefinitionId),
            WaitForCompletion = new(false)
        };

        var (context, _) = await ExecuteAsync(dispatchWorkflow);

        var result = context.GetActivityOutput(() => dispatchWorkflow.Result);
        Assert.Equal(DefaultGeneratedId, result);
    }

    [Fact]
    public async Task Should_Dispatch_Workflow_With_Generated_Instance_Id()
    {
        var dispatchWorkflow = new DispatchWorkflow
        {
            WorkflowDefinitionId = new(DefaultWorkflowDefinitionId),
            WaitForCompletion = new(false)
        };

        var (_, workflowDispatcher) = await ExecuteAsync(dispatchWorkflow);

        await workflowDispatcher.Received(1).DispatchAsync(
            Arg.Is<DispatchWorkflowDefinitionRequest>(request => request.InstanceId == DefaultGeneratedId),
            Arg.Any<DispatchWorkflowOptions>(),
            Arg.Any<CancellationToken>());
    }

    private static WorkflowGraph CreateMockWorkflowGraph()
    {
        var writeLine = new WriteLine("Test") { Id = "test-activity" };
        var workflow = Workflow.FromActivity(writeLine);
        workflow.Id = "test-workflow";
        var rootNode = new ActivityNode(writeLine, "Done");
        return new(workflow, rootNode, [rootNode]);
    }

    private static IWorkflowDefinitionService CreateWorkflowDefinitionService(string workflowDefinitionId, WorkflowGraph workflowGraph)
    {
        var service = Substitute.For<IWorkflowDefinitionService>();
        service
            .FindWorkflowGraphAsync(workflowDefinitionId, Arg.Any<VersionOptions>(), Arg.Any<CancellationToken>())
            .Returns(workflowGraph);
        return service;
    }

    private static IIdentityGenerator CreateIdentityGenerator(string generatedId)
    {
        var generator = Substitute.For<IIdentityGenerator>();
        generator.GenerateId().Returns(generatedId);
        return generator;
    }

    private static IWorkflowDispatcher CreateWorkflowDispatcher()
    {
        var dispatcher = Substitute.For<IWorkflowDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<DispatchWorkflowDefinitionRequest>(), Arg.Any<DispatchWorkflowOptions>(), Arg.Any<CancellationToken>())
            .Returns(DispatchWorkflowResponse.Success());
        return dispatcher;
    }

    private static async Task<(ActivityExecutionContext Context, IWorkflowDispatcher WorkflowDispatcher)> ExecuteAsync(DispatchWorkflow dispatchWorkflow)
    {
        var workflowGraph = CreateMockWorkflowGraph();
        var workflowDefinitionService = CreateWorkflowDefinitionService(DefaultWorkflowDefinitionId, workflowGraph);
        var identityGenerator = CreateIdentityGenerator(DefaultGeneratedId);
        var workflowDispatcher = CreateWorkflowDispatcher();

        var context = await new ActivityTestFixture(dispatchWorkflow)
            .ConfigureServices(services =>
            {
                services.AddSingleton(workflowDefinitionService);
                services.AddSingleton(identityGenerator);
                services.AddSingleton(workflowDispatcher);
            })
            .ExecuteAsync();

        return (context, workflowDispatcher);
    }
}

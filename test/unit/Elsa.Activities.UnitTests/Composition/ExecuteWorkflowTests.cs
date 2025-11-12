using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Composition;

public class ExecuteWorkflowTests
{
    private const string DefaultWorkflowDefinitionId = "test-workflow-def";
    private const string DefaultGeneratedId = "generated-child-id";
    private const string DefaultCorrelationId = "test-correlation";

    [Theory]
    [InlineData(true, WorkflowStatus.Finished)]
    [InlineData(true, WorkflowStatus.Running)]
    [InlineData(false, WorkflowStatus.Finished)]
    [InlineData(false, WorkflowStatus.Running)]
    public async Task Should_Invoke_Workflow_With_Correct_Options(bool waitForCompletion, WorkflowStatus childWorkflowStatus)
    {
        // Arrange
        var input = new Dictionary<string, object> { ["Key1"] = "Value1" };
        var childOutput = new Dictionary<string, object> { ["ChildKey"] = "ChildValue" };

        var executeWorkflow = new ExecuteWorkflow
        {
            WorkflowDefinitionId = new(DefaultWorkflowDefinitionId),
            CorrelationId = new(DefaultCorrelationId),
            Input = new(input),
            WaitForCompletion = new(waitForCompletion)
        };

        // Act
        var (context, workflowInvoker) = await ExecuteAsync(executeWorkflow, childWorkflowStatus, childOutput);
        var parentInstanceId = context.WorkflowExecutionContext.Id;

        // Assert
        await workflowInvoker.Received(1).InvokeAsync(
            Arg.Any<WorkflowGraph>(),
            Arg.Is<RunWorkflowOptions>(opts =>
                opts.ParentWorkflowInstanceId == parentInstanceId &&
                opts.WorkflowInstanceId == DefaultGeneratedId &&
                opts.CorrelationId == DefaultCorrelationId &&
                opts.Input != null && opts.Input.ContainsKey("Key1") && (string)opts.Input["Key1"] == "Value1" &&
                opts.Input.ContainsKey("ParentInstanceId") && (string)opts.Input["ParentInstanceId"] == parentInstanceId &&
                opts.Properties!.ContainsKey("ParentInstanceId") && (string)opts.Properties["ParentInstanceId"] == parentInstanceId &&
                (waitForCompletion ? opts.Properties.ContainsKey("WaitForCompletion") && (bool)opts.Properties["WaitForCompletion"] : !opts.Properties.ContainsKey("WaitForCompletion"))
            ),
            Arg.Any<CancellationToken>()
        );

        // Result is only set when either not waiting or child workflow finishes
        if (!waitForCompletion || childWorkflowStatus == WorkflowStatus.Finished)
        {
            var result = (ExecuteWorkflowResult)context.GetActivityOutput(() => executeWorkflow.Result)!;
            Assert.NotNull(result);
            Assert.Equal(DefaultGeneratedId, result.WorkflowInstanceId);
            Assert.Equal(childWorkflowStatus, result.Status);
            Assert.Equal(childOutput, result.Output);
        }
    }

    [Theory]
    [InlineData(false, WorkflowStatus.Running)]
    [InlineData(true, WorkflowStatus.Finished)]
    public async Task Should_Complete_Activity_When_Expected(bool waitForCompletion, WorkflowStatus childWorkflowStatus)
    {
        // Arrange
        var executeWorkflow = new ExecuteWorkflow
        {
            WorkflowDefinitionId = new(DefaultWorkflowDefinitionId),
            WaitForCompletion = new(waitForCompletion)
        };

        // Act
        var (context, _) = await ExecuteAsync(executeWorkflow, childWorkflowStatus);

        // Assert - Activity should complete when not waiting or when child workflow finishes
        Assert.True(context.IsCompleted);
    }

    [Fact]
    public async Task Should_Create_Bookmark_When_WaitForCompletion_Is_True_And_Child_Is_Running()
    {
        // Arrange
        var executeWorkflow = new ExecuteWorkflow
        {
            WorkflowDefinitionId = new(DefaultWorkflowDefinitionId),
            WaitForCompletion = new(true)
        };

        // Act
        var (context, _) = await ExecuteAsync(executeWorkflow, WorkflowStatus.Running);

        // Assert - Activity should not complete when child workflow is still running and waiting
        Assert.False(context.IsCompleted);
        Assert.NotEmpty(context.Bookmarks);
    }

    [Fact]
    public async Task Should_Throw_When_Workflow_Definition_Not_Found()
    {
        // Arrange
        const string workflowDefinitionId = "non-existent-workflow";

        var workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
        workflowDefinitionService
            .FindWorkflowGraphAsync(workflowDefinitionId, Arg.Any<VersionOptions>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowGraph?)null);

        var executeWorkflow = new ExecuteWorkflow
        {
            WorkflowDefinitionId = new(workflowDefinitionId)
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => ExecuteAsync(executeWorkflow, customWorkflowDefinitionService: workflowDefinitionService));
    }

    [Fact]
    public async Task Should_Use_Empty_Input_When_Input_Not_Provided()
    {
        // Arrange
        var executeWorkflow = new ExecuteWorkflow
        {
            WorkflowDefinitionId = new(DefaultWorkflowDefinitionId)
        };

        // Act
        var (_, workflowInvoker) = await ExecuteAsync(executeWorkflow);

        // Assert - Should create input with ParentInstanceId even when no input provided
        await workflowInvoker.Received(1).InvokeAsync(
            Arg.Any<WorkflowGraph>(),
            Arg.Is<RunWorkflowOptions>(opts =>
                opts.Input != null &&
                opts.Input.ContainsKey("ParentInstanceId") &&
                opts.Input.Count == 1
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_Use_Null_CorrelationId_When_Not_Provided()
    {
        // Arrange
        var executeWorkflow = new ExecuteWorkflow
        {
            WorkflowDefinitionId = new(DefaultWorkflowDefinitionId)
        };

        // Act
        var (_, workflowInvoker) = await ExecuteAsync(executeWorkflow);

        // Assert
        await workflowInvoker.Received(1).InvokeAsync(
            Arg.Any<WorkflowGraph>(),
            Arg.Is<RunWorkflowOptions>(opts => opts.CorrelationId == null),
            Arg.Any<CancellationToken>()
        );
    }

    [Theory]
    [InlineData(WorkflowSubStatus.Cancelled)]
    [InlineData(WorkflowSubStatus.Faulted)]
    [InlineData(WorkflowSubStatus.Suspended)]
    public async Task Should_Handle_Different_Workflow_SubStatuses(WorkflowSubStatus subStatus)
    {
        // Arrange
        var executeWorkflow = new ExecuteWorkflow
        {
            WorkflowDefinitionId = new(DefaultWorkflowDefinitionId),
            WaitForCompletion = new(false)
        };

        // Act
        var (context, _) = await ExecuteAsync(executeWorkflow, WorkflowStatus.Finished, null, subStatus);

        // Assert
        var result = (ExecuteWorkflowResult)context.GetActivityOutput(() => executeWorkflow.Result)!;
        Assert.NotNull(result);
        Assert.Equal(subStatus, result.SubStatus);
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

    private static IWorkflowInvoker CreateWorkflowInvoker(
        WorkflowStatus status,
        IDictionary<string, object>? output,
        WorkflowSubStatus subStatus = WorkflowSubStatus.Executing)
    {
        var invoker = Substitute.For<IWorkflowInvoker>();
        var workflow = Workflow.FromActivity(new WriteLine("Test"));
        var workflowState = new WorkflowState
        {
            Status = status,
            SubStatus = subStatus,
            Output = output ?? new Dictionary<string, object>()
        };
        var workflowResult = new RunWorkflowResult(null!, workflowState, workflow, null, Journal.Empty);

        invoker
            .InvokeAsync(Arg.Any<WorkflowGraph>(), Arg.Any<RunWorkflowOptions>(), Arg.Any<CancellationToken>())
            .Returns(workflowResult);

        return invoker;
    }

    private static async Task<(ActivityExecutionContext Context, IWorkflowInvoker WorkflowInvoker)>
        ExecuteAsync(
            ExecuteWorkflow executeWorkflow,
            WorkflowStatus childWorkflowStatus = WorkflowStatus.Finished,
            IDictionary<string, object>? output = null,
            WorkflowSubStatus subStatus = WorkflowSubStatus.Executing,
            IWorkflowDefinitionService? customWorkflowDefinitionService = null,
            IWorkflowInvoker? customWorkflowInvoker = null,
            IIdentityGenerator? customIdentityGenerator = null)
    {
        var workflowGraph = CreateMockWorkflowGraph();
        var workflowDefinitionService = customWorkflowDefinitionService ?? CreateWorkflowDefinitionService(DefaultWorkflowDefinitionId, workflowGraph);
        var identityGenerator = customIdentityGenerator ?? CreateIdentityGenerator(DefaultGeneratedId);
        var workflowInvoker = customWorkflowInvoker ?? CreateWorkflowInvoker(childWorkflowStatus, output, subStatus);

        var context = await new ActivityTestFixture(executeWorkflow)
            .ConfigureServices(services =>
            {
                services.AddSingleton(workflowDefinitionService);
                services.AddSingleton(workflowInvoker);
                services.AddSingleton(identityGenerator);
                services.AddSingleton<IStimulusHasher>(_ => Substitute.For<IStimulusHasher>());
            })
            .ExecuteAsync();

        return (context, workflowInvoker);
    }

}

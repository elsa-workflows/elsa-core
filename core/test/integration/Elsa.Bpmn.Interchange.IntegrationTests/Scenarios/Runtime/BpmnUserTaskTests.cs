using System.Text.Json;
using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Authorization;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Extensions;
using Elsa.UserTasks.Activities;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Permissions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime.BpmnRuntimeTestHost;
using UserTask = Elsa.UserTasks.Activities.UserTask;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime;

/// <summary>
/// A BPMN <c>userTask</c> bound, through the <c>elsa:activityBinding</c> extension, to the User Tasks module's own
/// <see cref="UserTask"/> activity, and completed the way a person completes one: through
/// <see cref="IUserTaskManager"/>, which is what the User Tasks API calls.
/// </summary>
/// <remarks>
/// The document declares the binding here, to configure the task's assignee and actions; a <c>userTask</c> that declares
/// none binds to a plain <see cref="UserTask"/>, which <see cref="BpmnUserTaskDefaultBindingTests"/> covers. What is
/// worth running is everything after the binding: the bound activity's bookmark is projected into a task, and completing
/// the task resumes the BPMN scope that is waiting on it.
/// </remarks>
public class BpmnUserTaskTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private const string ProcessId = "approve-order";
    private const string Provider = "test";

    private readonly BpmnRuntimeTestHost _host = new(testOutputHelper, elsa => elsa.UseUserTasks());

    public Task InitializeAsync() => _host.InitializeAsync();

    public Task DisposeAsync() => _host.DisposeAsync();

    [Fact(DisplayName = "A userTask bound to UserTask becomes a task, and completing that task resumes the process")]
    public async Task UserTask_BoundThroughTheActivityBinding_IsCompletedThroughTheUserTasksManager()
    {
        // Arrange: the process waits on its user task.
        await _host.PublishAsync(ProcessId, BindProcess());
        var instanceId = await _host.StartAsync(ProcessId);

        var waiting = await _host.StateOfAsync(instanceId);
        Assert.True(IsSuspended(waiting));

        var bookmark = Assert.Single(waiting.Bookmarks);
        Assert.Equal(WorkActivityId(ProcessId, "Approve"), bookmark.ActivityId);

        // The bookmark was projected into a task, assigned to the person the binding names.
        var tenantId = TenantOf(bookmark);
        var repository = _host.Services.GetRequiredService<IUserTaskRepository>();
        var task = Assert.IsType<Elsa.UserTasks.Models.UserTask>(await repository.FindByBookmarkIdAsync(tenantId, bookmark.Id));

        Assert.Equal("Approve order", task.Title);
        Assert.Equal(instanceId, task.WorkflowInstanceId);
        Assert.True(task.Assignee?.Matches(Alice(tenantId)));
        Assert.Equal(["Approve", "Reject"], task.Actions.Select(action => action.Key));

        // Act: the assignee completes it.
        using (var scope = _host.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<IUserTaskManager>();
            var outcome = await manager.CompleteAsync(tenantId, task.Id, new UserTaskCompletionRequest(task.Revision, "complete-1", "Approve"), AliceActor(tenantId));

            Assert.True(outcome.Accepted, outcome.ConflictCode);
        }

        // Assert: the process moved past the user task and finished...
        var finished = await _host.StateOfAsync(instanceId);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Empty(finished.Incidents);
        Assert.Empty(finished.Bookmarks);

        var statuses = await _host.ActivityStatusesOfAsync(instanceId);
        Assert.Equal(ActivityStatus.Completed, statuses[WorkActivityId(ProcessId, "Approve")]);
        Assert.Equal(ActivityStatus.Completed, statuses[WorkActivityId(ProcessId, "Ship")]);

        // ...and the task records who completed it, and with which action.
        var completed = Assert.IsType<Elsa.UserTasks.Models.UserTask>(await repository.GetAsync(tenantId, task.Id));
        Assert.Equal(UserTaskStatus.Completed, completed.Status);
        Assert.Equal("Approve", completed.CompletionActionKey);
        Assert.True(completed.CompletedBy?.Matches(Alice(tenantId)));
    }

    [Theory(DisplayName = "The action a person completes a userTask with selects the sequence flow named for it")]
    [InlineData("Approve", "Ship", "Notify")]
    [InlineData("Reject", "Notify", "Ship")]
    public async Task UserTask_CompletedWithAnAction_TakesTheFlowForThatAction(string action, string taken, string notTaken)
    {
        await _host.PublishAsync(ProcessId, BindDecisionProcess());
        var instanceId = await _host.StartAsync(ProcessId);

        await CompleteTaskAsync(instanceId, action);

        var finished = await _host.StateOfAsync(instanceId);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Empty(finished.Incidents);

        var statuses = await _host.ActivityStatusesOfAsync(instanceId);
        Assert.Equal(ActivityStatus.Completed, statuses[WorkActivityId(ProcessId, taken)]);
        Assert.False(statuses.ContainsKey(WorkActivityId(ProcessId, notTaken)), $"'{notTaken}' ran, though '{action}' does not select its flow.");
    }

    /// <summary>Completes the instance's one waiting user task with <paramref name="action"/>, as its assignee.</summary>
    private async Task CompleteTaskAsync(string instanceId, string action)
    {
        var bookmark = Assert.Single((await _host.StateOfAsync(instanceId)).Bookmarks);
        var tenantId = TenantOf(bookmark);
        var task = Assert.IsType<Elsa.UserTasks.Models.UserTask>(await _host.Services.GetRequiredService<IUserTaskRepository>().FindByBookmarkIdAsync(tenantId, bookmark.Id));

        using var scope = _host.Services.CreateScope();
        var outcome = await scope.ServiceProvider.GetRequiredService<IUserTaskManager>()
            .CompleteAsync(tenantId, task.Id, new UserTaskCompletionRequest(task.Revision, $"complete-{action}", action), AliceActor(tenantId));
        Assert.True(outcome.Accepted, outcome.ConflictCode);
    }

    /// <summary>A userTask whose two outgoing flows are selected by the action it is completed with.</summary>
    private BpmnProcess BindDecisionProcess()
    {
        var format = _host.Services.GetRequiredService<BpmnActivityBindingFormat>();
        var approve = new UserTask("Approve order")
        {
            Assignee = new(Alice(DefaultTenantId)),
            ActionDefinitions = new(new List<UserTaskActionDefinition> { new("Approve", "Approve"), new("Reject", "Reject") })
        };

        var definition = new BpmnProcessBuilder(ProcessId)
            .StartEvent("Start")
            .Element(Bound(format, "Approve", BpmnElementTypes.UserTask, approve))
            .Element(Bound(format, "Ship", BpmnElementTypes.ServiceTask, new WriteLine("Shipping")))
            .Element(Bound(format, "Notify", BpmnElementTypes.ServiceTask, new WriteLine("Rejected")))
            .EndEvent("Shipped")
            .EndEvent("Rejected")
            .Connect("Start", "Approve")
            .Connect("Approve", "Ship", condition: "Approve")
            .Connect("Approve", "Notify", condition: "Reject")
            .Connect("Ship", "Shipped")
            .Connect("Notify", "Rejected")
            .Build();

        return _host.Services.GetRequiredService<BpmnWorkBinder>().Bind(definition,
            [Unbound("Approve", BpmnElementTypes.UserTask), Unbound("Ship", BpmnElementTypes.ServiceTask), Unbound("Notify", BpmnElementTypes.ServiceTask)]);
    }

    /// <summary>
    /// The process, bound by <see cref="BpmnWorkBinder"/> from a definition whose <c>userTask</c> carries an
    /// <c>elsa:activityBinding</c> naming <see cref="UserTask"/>, exactly as an imported document would.
    /// </summary>
    private BpmnProcess BindProcess()
    {
        var format = _host.Services.GetRequiredService<BpmnActivityBindingFormat>();

        // The assignee's tenant is only known once the task is projected, so the binding names the person by the
        // tenant every unconfigured host resolves to: the workflow's own, which is the default one.
        var approve = new UserTask("Approve order")
        {
            Assignee = new(Alice(DefaultTenantId)),
            ActionDefinitions = new(new List<UserTaskActionDefinition> { new("Approve", "Approve"), new("Reject", "Reject") })
        };

        var definition = new BpmnProcessBuilder(ProcessId)
            .StartEvent("Start")
            .Element(Bound(format, "Approve", BpmnElementTypes.UserTask, approve))
            .Element(Bound(format, "Ship", BpmnElementTypes.ServiceTask, new WriteLine("Shipping")))
            .EndEvent("End")
            .ConnectSequence("Start", "Approve", "Ship", "End")
            .Build();

        return _host.Services.GetRequiredService<BpmnWorkBinder>().Bind(definition, [Unbound("Approve", BpmnElementTypes.UserTask), Unbound("Ship", BpmnElementTypes.ServiceTask)]);
    }

    private const string DefaultTenantId = "";

    private static BpmnElement Bound(BpmnActivityBindingFormat format, string elementId, string elementType, IActivity activity) =>
        new(elementId, elementType, bindingRef: Ref(elementId), extensions: BpmnActivityBindingFormat.Attach(null, format.Write(activity)));

    private static BpmnWorkBinding.UnboundTask Unbound(string elementId, string elementType) =>
        new(ProcessId, elementId, Ref(elementId), BpmnBindingSlot.Primary, elementType);

    private static string Ref(string elementId) => $"{BpmnXmlReader.DefaultBindingRefPrefix}-{elementId}";

    private static ParticipantReference Alice(string tenantId) => new(tenantId, Provider, UserTaskParticipantType.User, "alice");

    private static UserTaskActor AliceActor(string tenantId) => new(Alice(tenantId), [])
    {
        Permissions = new HashSet<string>(
            [new Permission(UserTasksResourcePermissions.UserTasks, CoreVerbs.View).ToString(), new Permission(UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Complete).ToString()],
            StringComparer.Ordinal)
    };

    /// <summary>The tenant the task was projected under, read from the bookmark the <see cref="UserTask"/> created.</summary>
    private static string TenantOf(Bookmark bookmark)
    {
        var payload = JsonSerializer.SerializeToElement(bookmark.Payload);
        var tenant = payload.EnumerateObject().First(property => string.Equals(property.Name, "tenantId", StringComparison.OrdinalIgnoreCase)).Value;

        return tenant.GetString() ?? DefaultTenantId;
    }
}

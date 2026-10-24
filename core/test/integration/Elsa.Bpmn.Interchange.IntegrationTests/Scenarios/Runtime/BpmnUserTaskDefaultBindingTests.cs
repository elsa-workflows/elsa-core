using System.Text.Json;
using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Extensions;
using Elsa.UserTasks.Contracts;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime.BpmnRuntimeTestHost;
using UserTask = Elsa.UserTasks.Activities.UserTask;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime;

/// <summary>
/// A BPMN <c>userTask</c> that declares no <c>elsa:activityBinding</c> binds to the User Tasks module's own
/// <see cref="UserTask"/> when that module is enabled, titled with the element's name.
/// </summary>
public class BpmnUserTaskDefaultBindingTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private const string ProcessId = "approve-order";

    private readonly BpmnRuntimeTestHost _host = new(testOutputHelper, elsa => elsa.UseUserTasks());

    public Task InitializeAsync() => _host.InitializeAsync();

    public Task DisposeAsync() => _host.DisposeAsync();

    [Fact(DisplayName = "A userTask that declares no binding becomes a task titled with the element's name")]
    public async Task UserTask_WithNoDeclaration_BecomesATaskTitledWithItsName()
    {
        var definition = new BpmnProcessBuilder(ProcessId)
            .StartEvent("Start")
            .Element(new BpmnElement("Approve", BpmnElementTypes.UserTask, name: "Approve order", bindingRef: Ref("Approve")))
            .EndEvent("End")
            .ConnectSequence("Start", "Approve", "End")
            .Build();

        var process = _host.Services.GetRequiredService<BpmnWorkBinder>()
            .Bind(definition, [new BpmnWorkBinding.UnboundTask(ProcessId, "Approve", Ref("Approve"), BpmnBindingSlot.Primary, BpmnElementTypes.UserTask)]);

        Assert.IsType<UserTask>(Assert.Single(process.Activities));

        await _host.PublishAsync(ProcessId, process);
        var instanceId = await _host.StartAsync(ProcessId);

        var bookmark = Assert.Single((await _host.StateOfAsync(instanceId)).Bookmarks);
        Assert.Equal(WorkActivityId(ProcessId, "Approve"), bookmark.ActivityId);

        var task = await _host.Services.GetRequiredService<IUserTaskRepository>().FindByBookmarkIdAsync(TenantOf(bookmark), bookmark.Id);
        Assert.Equal("Approve order", Assert.IsType<Elsa.UserTasks.Models.UserTask>(task).Title);
    }

    /// <summary>The tenant the task was projected under, read from the bookmark the <see cref="UserTask"/> created.</summary>
    private static string TenantOf(Bookmark bookmark)
    {
        var payload = JsonSerializer.SerializeToElement(bookmark.Payload);
        var tenant = payload.EnumerateObject().First(property => string.Equals(property.Name, "tenantId", StringComparison.OrdinalIgnoreCase)).Value;

        return tenant.GetString() ?? "";
    }

    private static string Ref(string elementId) => $"{BpmnXmlReader.DefaultBindingRefPrefix}-{elementId}";
}

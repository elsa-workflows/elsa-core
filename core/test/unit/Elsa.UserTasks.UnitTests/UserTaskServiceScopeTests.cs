using Elsa.Mediator.Contracts;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Extensions;
using Elsa.UserTasks.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UserTask = Elsa.UserTasks.Models.UserTask;

namespace Elsa.UserTasks.UnitTests;

/// <summary>
/// The default workflow resumer and notification sink use scoped runtime services, so they must resolve them from
/// the caller's scope, also when the container validates scopes. Regression for elsa-workflows/elsa-core#9111.
/// </summary>
public sealed class UserTaskServiceScopeTests
{
    [Fact(DisplayName = "The default workflow resumer resumes through the IWorkflowResumer of the caller's scope")]
    public async Task DefaultWorkflowResumer_UsesTheCallersScope()
    {
        await using var serviceProvider = BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var workflowResumer = scope.ServiceProvider.GetRequiredService<IWorkflowResumer>();
        var task = new UserTask { TenantId = "tenant", BookmarkId = "bookmark-1" };
        var stimulus = new UserTaskStimulus("tenant", task.Id, "operation-1", "Approve", null);

        await scope.ServiceProvider.GetRequiredService<IUserTaskWorkflowResumer>().ResumeAsync(task, stimulus);

        await workflowResumer.Received(1).ResumeAsync(
            "bookmark-1",
            Arg.Is<IDictionary<string, object>>(input => ReferenceEquals(input[Activities.UserTask.InputKey], stimulus)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "The default notification sink publishes through the INotificationSender of the caller's scope")]
    public async Task DefaultNotificationSink_UsesTheCallersScope()
    {
        await using var serviceProvider = BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
        var notification = new UserTaskCreated("tenant", "task-1", UserTaskStatus.Available, 1);

        await scope.ServiceProvider.GetRequiredService<IUserTaskNotificationSink>().PublishAsync(notification);

        await notificationSender.Received(1).SendAsync(notification, Arg.Any<CancellationToken>());
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddUserTasksServices();
        services.AddScoped(_ => Substitute.For<IWorkflowResumer>());
        services.AddScoped(_ => Substitute.For<INotificationSender>());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}

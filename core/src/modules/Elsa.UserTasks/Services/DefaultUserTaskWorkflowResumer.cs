using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Services;

public sealed class DefaultUserTaskWorkflowResumer(IServiceProvider services) : IUserTaskWorkflowResumer
{
    public async Task ResumeAsync(UserTask task, UserTaskStimulus stimulus, CancellationToken cancellationToken = default)
    {
        var resumer = services.GetService<IWorkflowResumer>();
        if (resumer == null)
            throw new InvalidOperationException("A workflow resumer is required to complete a User Task.");

        await resumer.ResumeAsync(task.BookmarkId, new Dictionary<string, object>
        {
            [Elsa.UserTasks.Activities.UserTask.InputKey] = stimulus
        }, cancellationToken);
    }
}

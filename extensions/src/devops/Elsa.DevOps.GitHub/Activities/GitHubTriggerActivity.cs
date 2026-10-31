using Elsa.Workflows;

namespace Elsa.DevOps.GitHub.Activities;

/// <summary>
/// Generic base class inherited by all GitHub trigger activities.
/// </summary>
public abstract class GitHubTriggerActivity : GitHubActivity
{
    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Implementation depends on GitHub's Events API and WebHook support
        throw new NotImplementedException("Event subscription requires WebHook implementation.");
    }
}
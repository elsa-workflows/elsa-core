using Elsa.DevOps.GitHub.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Octokit;

namespace Elsa.DevOps.GitHub.Activities;

/// <summary>
/// Generic base class inherited by all GitHub activities.
/// </summary>
public abstract class GitHubActivity : Workflows.Activity
{
    /// <summary>
    /// The GitHub API token.
    /// </summary>
    [Input(Description = "The GitHub API token.")]
    public Input<string> Token { get; set; } = null!;

    /// <summary>
    /// Gets the GitHub API client.
    /// </summary>
    /// <param name="context">The current context to get the client.</param>
    /// <returns>The GitHub API client.</returns>
    protected IGitHubClient GetClient(ActivityExecutionContext context)
    {
        GitHubClientFactory githubClientFactory = context.GetRequiredService<GitHubClientFactory>();
        string token = context.Get(Token)!;
        return githubClientFactory.GetClient(token);
    }
}
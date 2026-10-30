# Elsa.DevOps.GitHub

This module provides integration with GitHub for Elsa Workflows. It enables workflows to interact with GitHub repositories, issues, pull requests, comments, users, organizations, and more.

## Features

- Authentication via personal access tokens
- Activities for GitHub issues, comments, pull requests, repositories, users, and organizations
- Support for webhooks via trigger activities
- GraphQL API query support

## Getting Started

### Installation

Add the Elsa GitHub extension to your project:

```bash
dotnet add package Elsa.DevOps.GitHub
```

### Registration

Register the GitHub extension in your Elsa builder:

```csharp
// Program.cs or Startup.cs
services
    .AddElsa(elsa => 
    {
        elsa.UseGitHub();
        // Other Elsa configurations...
    });
```

## Authentication

The GitHub activities require a personal access token for authentication. You can create a token in your GitHub account under Settings > Developer settings > Personal access tokens.

The token is passed to each activity and should be stored securely using the Elsa Secrets management system.

## Available Activities

### Issues

| Activity | Description |
|----------|-------------|
| CreateIssue | Creates a new issue in a repository |
| GetIssue | Retrieves details of an existing issue |
| UpdateIssue | Updates an existing issue |
| DeleteIssue | Removes an existing issue (by closing it) |
| SearchIssues | Searches for issues in repositories |

### Comments

| Activity | Description |
|----------|-------------|
| CreateComment | Creates a new comment |
| GetComment | Retrieves a specific comment |
| UpdateComment | Updates an existing comment |
| DeleteComment | Deletes a comment |

### Labels

| Activity | Description |
|----------|-------------|
| AddLabels | Adds labels to an issue or pull request |
| DeleteLabel | Removes a label from an issue or pull request |

### Pull Requests

| Activity | Description |
|----------|-------------|
| GetPullRequest | Retrieves details of an existing pull request |
| SearchPullRequests | Searches for pull requests |

### Repositories

| Activity | Description |
|----------|-------------|
| GetRepository | Retrieves details of a repository |
| GetBranch | Retrieves details of a specific branch |
| SearchBranches | Lists or searches branches in a repository |

### Users

| Activity | Description |
|----------|-------------|
| AddAssignees | Adds assignees to an issue or pull request |
| DeleteAssignees | Removes assignees from an issue or pull request |
| GetUser | Retrieves details of a user |
| GetAssignee | Checks if a user can be assigned to issues |
| SearchAssignees | Lists assignees for issues in a repository |

### Organizations

| Activity | Description |
|----------|-------------|
| GetOrganization | Retrieves details of an organization |
| SearchOrganizationMembers | Lists or searches members of an organization |

### Milestones

| Activity | Description |
|----------|-------------|
| GetMilestone | Retrieves details of a specific milestone |
| SearchMilestones | Searches for milestones in a repository |

### Releases

| Activity | Description |
|----------|-------------|
| GetRelease | Retrieves details of a specific release |
| SearchReleases | Searches for releases in a repository |

### Gists

| Activity | Description |
|----------|-------------|
| GetGist | Retrieves a GitHub Gist by its ID |
| SearchGists | Searches for GitHub Gists |

### Events (Triggers)

| Activity | Description |
|----------|-------------|
| WatchIssues | Triggers when an issue is created or updated |
| WatchPullRequests | Triggers when a pull request is created or updated |
| WatchBranches | Triggers when a branch is created |

### Other

| Activity | Description |
|----------|-------------|
| ExecuteGraphQLQuery | Performs a GraphQL query against the GitHub API |

## Example Usage

Here's an example of creating a GitHub issue from a workflow:

```csharp
// Create a workflow definition
public class CreateGitHubIssueWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .StartWith<CreateIssue>(activity =>
            {
                activity.Owner = "elsa-workflows";
                activity.Repository = "elsa-extensions";
                activity.Title = "Automatically created issue";
                activity.Body = "This issue was created by an Elsa workflow.";
                activity.Labels = new[] { "automated", "elsa-workflow" };
                activity.Token = new Input<string>("your-github-token");
            })
            .Then<WriteRawOutput>(activity =>
            {
                activity.Content = new JavaScriptValue<object>("return `Created issue #${createIssue.createdIssue.number}`;");
            });
    }
}
```

## Notes

- Event activities (WatchIssues, WatchPullRequests, etc.) require webhook registration and implementation, which depends on your specific hosting environment.
- For production usage, it's recommended to store GitHub tokens securely using Elsa's secret management features.
- Rate limits apply when using the GitHub API. Refer to [GitHub's rate limiting documentation](https://docs.github.com/en/rest/overview/resources-in-the-rest-api#rate-limiting) for more details.

## References

- [GitHub API Documentation](https://docs.github.com/en/rest)
- [Octokit.NET Documentation](https://github.com/octokit/octokit.net)
using Octokit;

namespace Elsa.DevOps.GitHub.Services;

/// <summary>
/// Factory for creating GitHub API clients.
/// </summary>
public class GitHubClientFactory
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, IGitHubClient> _githubClients = new();

    /// <summary>
    /// Gets a GitHub API client for the specified token.
    /// </summary>
    public IGitHubClient GetClient(string token)
    {
        if (_githubClients.TryGetValue(token, out IGitHubClient? client))
            return client;

        try
        {
            _semaphore.Wait();

            if (_githubClients.TryGetValue(token, out client))
                return client;

            var newClient = new GitHubClient(new ProductHeaderValue("Elsa-Workflow-GitHub-Integration"));
            newClient.Credentials = new Credentials(token);
            
            _githubClients[token] = newClient;
            return newClient;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
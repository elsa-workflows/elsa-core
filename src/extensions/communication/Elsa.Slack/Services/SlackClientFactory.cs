using SlackNet;

namespace Elsa.Slack.Services;

/// <summary>
/// Factory for creating Slack API clients.
/// </summary>
public class SlackClientFactory
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, ISlackApiClient> _slackClients = new();

    /// <summary>
    /// Gets a Slack API client for the specified token.
    /// </summary>
    public ISlackApiClient GetClient(string token)
    {
        if (_slackClients.TryGetValue(token, out ISlackApiClient? client))
            return client;

        try
        {
            _semaphore.Wait();

            if (_slackClients.TryGetValue(token, out client))
                return client;

            SlackApiClient newClient = new(token);
            _slackClients[token] = newClient;
            return newClient;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
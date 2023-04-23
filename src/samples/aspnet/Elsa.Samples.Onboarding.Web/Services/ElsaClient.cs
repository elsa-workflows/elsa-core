namespace Elsa.Samples.Onboarding.Web.Services;

/// <summary>
/// A client for the Elsa API.
/// </summary>
public class ElsaClient
{
    private readonly HttpClient _httpClient;

    public ElsaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Reports a task as completed.
    /// </summary>
    /// <param name="taskId">The ID of the task to complete.</param>
    /// <param name="result">The result of the task.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task ReportTaskCompletedAsync(string taskId, object? result = default, CancellationToken cancellationToken = default)
    {
        var url = new Uri($"tasks/{taskId}/complete", UriKind.Relative);
        var request = new { Result = result };
        await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
    }
}
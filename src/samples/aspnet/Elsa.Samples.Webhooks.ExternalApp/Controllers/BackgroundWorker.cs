using Elsa.Samples.Webhooks.ExternalApp.Models;

namespace Elsa.Samples.Webhooks.ExternalApp.Controllers;

public class BackgroundWorker
{
    private readonly HttpClient _httpClient;

    public BackgroundWorker(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task RunAsync(RunTaskPayload taskPayload)
    {
        var taskId = taskPayload.TaskId;
        await _httpClient.PostAsJsonAsync($"tasks/{taskId}/complete", new { Foo = "Bar"  });
    }
}
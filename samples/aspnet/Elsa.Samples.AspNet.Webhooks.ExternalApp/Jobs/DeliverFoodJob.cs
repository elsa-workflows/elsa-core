using Elsa.Samples.AspNet.Webhooks.ExternalApp.Models;

namespace Elsa.Samples.AspNet.Webhooks.ExternalApp.Jobs;

public class DeliverFoodJob
{
    private readonly HttpClient _httpClient;

    public DeliverFoodJob(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task RunAsync(RunTaskPayload taskPayload)
    {
        var taskId = taskPayload.TaskId;
        await _httpClient.PostAsJsonAsync($"tasks/{taskId}/complete", new { Result = "Pizza"  });
    }
}
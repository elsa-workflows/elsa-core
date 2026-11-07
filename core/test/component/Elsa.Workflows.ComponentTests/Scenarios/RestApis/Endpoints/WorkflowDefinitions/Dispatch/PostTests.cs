using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.WorkflowDefinitions.Dispatch;

public class PostTests(App app) : AppComponentTest(app)
{
    private const string Url = "workflow-definitions/{0}/dispatch";
    private const string NoPublishedVersionMessage = "has no published version";

    [Fact(DisplayName = "Dispatching a never-published definition returns 404 with a message")]
    public async Task NeverPublishedDefinition_ReturnsNotFoundWithMessage()
    {
        var publisher = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        var draft = await publisher.NewAsync(new WriteLine("Draft"));
        await publisher.SaveDraftAsync(draft);

        using var response = await DispatchAsync(draft.DefinitionId);
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(errors, error => error.StartsWith($"Workflow definition '{draft.DefinitionId}' {NoPublishedVersionMessage}"));
    }

    [Fact(DisplayName = "Dispatching an unknown definition returns 404 without the unpublished message")]
    public async Task UnknownDefinition_ReturnsNotFoundWithoutMessage()
    {
        using var response = await DispatchAsync(Guid.NewGuid().ToString("N"));
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(errors, error => error.Contains(NoPublishedVersionMessage));
    }

    private static async Task<IReadOnlyList<string>> ReadErrorsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(body))
            return [];

        using var document = JsonDocument.Parse(body);

        if (!document.RootElement.TryGetProperty("errors", out var errors))
            return [];

        return errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString()!).ToList();
    }

    private Task<HttpResponseMessage> DispatchAsync(string definitionId)
    {
        var client = WorkflowServer.CreateHttpClient();
        return client.PostAsJsonAsync(string.Format(Url, definitionId), new { });
    }
}

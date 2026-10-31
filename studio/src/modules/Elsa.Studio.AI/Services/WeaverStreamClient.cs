using Elsa.Studio.AI.Models;
using Elsa.Studio.Contracts;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Elsa.Studio.AI.Services;

public class WeaverStreamClient(HttpClient httpClient, IBackendApiClientProvider backendApiClientProvider)
{
    public async IAsyncEnumerable<WeaverStreamEvent> StreamChatAsync(WeaverChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(backendApiClientProvider.Url, "ai/chat"))
        {
            Content = JsonContent.Create(request, WeaverJsonContext.Default.WeaverChatRequest)
        };
        message.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        string? data = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null)
                break;

            if (line.Length == 0)
            {
                if (!string.IsNullOrWhiteSpace(data))
                {
                    var streamEvent = System.Text.Json.JsonSerializer.Deserialize(data, WeaverJsonContext.Default.WeaverStreamEvent);
                    if (streamEvent != null)
                        yield return streamEvent;
                }
                data = null;
                continue;
            }

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                data = string.IsNullOrEmpty(data) ? line[5..].TrimStart() : $"{data}\n{line[5..].TrimStart()}";
        }
    }
}

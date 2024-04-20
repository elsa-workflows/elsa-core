using System.Text.Json;
using Elsa.Api.Client;

namespace Elsa.Workflows.Api.ComponentTests;

public static class HttpResponseMessageExtensions
{
    public static async Task<T> ReadAsJsonAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var options = RefitSettingsHelper.CreateJsonSerializerOptions();
        return JsonSerializer.Deserialize<T>(json, options)!;
    }
}
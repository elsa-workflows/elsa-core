using System.Text.Json;
using Elsa.Api.Client;

// ReSharper disable once CheckNamespace
namespace Elsa.Workflows.ComponentTests;

public static class HttpResponseMessageExtensions
{
    public static async Task<T> ReadAsJsonAsync<T>(this HttpResponseMessage response, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var options = RefitSettingsHelper.CreateJsonSerializerOptions(serviceProvider);
        return JsonSerializer.Deserialize<T>(json, options)!;
    }
}
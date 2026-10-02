using System.Text.Json;
using Elsa.Telnyx.Serialization;

namespace Elsa.Telnyx.Helpers;

internal static class WebhookSerializerOptions
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = new SnakeCaseNamingPolicy()
        };
            
        options.Converters.Add(new WebhookDataJsonConverter());
        return options;
    }
}
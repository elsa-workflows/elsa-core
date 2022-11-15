using System.Text.Json;
using Dahomey.Json.NamingPolicies;
using Elsa.Telnyx.Converters;

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
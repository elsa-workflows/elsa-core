using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CShells.FastEndpoints.Contracts;
using Elsa.Workflows;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.FastEndpointConfigurators;

/// <summary>
/// Configures FastEndpoints with Elsa-specific serialization options.
/// Uses the same serialization settings as <see cref="Elsa.Extensions.WebApplicationExtensions.UseWorkflowsApi"/>.
/// </summary>
[UsedImplicitly]
public class ElsaFastEndpointsConfigurator : IFastEndpointsConfigurator
{
    /// <inheritdoc />
    public void Configure(Config config)
    {
        config.Serializer.RequestDeserializer = DeserializeRequestAsync;
        config.Serializer.ResponseSerializer = SerializeResponseAsync;

        config.Binding.ValueParserFor<DateTimeOffset>(s =>
            new(DateTimeOffset.TryParse(s.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result), result));
    }

    private static ValueTask<object?> DeserializeRequestAsync(HttpRequest httpRequest, Type modelType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
    {
        var serializer = httpRequest.HttpContext.RequestServices.GetRequiredService<IApiSerializer>();
        var options = serializer.GetOptions();

        return serializerContext == null
            ? JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, options, cancellationToken)
            : JsonSerializer.DeserializeAsync(httpRequest.Body, modelType, serializerContext, cancellationToken);
    }

    private static Task SerializeResponseAsync(HttpResponse httpResponse, object? dto, string contentType, JsonSerializerContext? serializerContext, CancellationToken cancellationToken)
    {
        var serializer = httpResponse.HttpContext.RequestServices.GetRequiredService<IApiSerializer>();
        var options = serializer.GetOptions();

        httpResponse.ContentType = contentType;
        return serializerContext == null
            ? JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto?.GetType() ?? typeof(object), options, cancellationToken)
            : JsonSerializer.SerializeAsync(httpResponse.Body, dto, dto?.GetType() ?? typeof(object), serializerContext, cancellationToken);
    }
}

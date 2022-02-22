using System.Buffers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace Elsa.Server.Api.Helpers;

public static class ObjectResultExtensions
{
    public static T ConfigureForWorkflowDefinition<T>(this T result) where T : ObjectResult
    {
        var settings = SerializationHelper.GetSettingsForWorkflowDefinition();
        return result.ConfigureFor(settings);
    }

    public static T ConfigureForEndpoint<T>(this T result) where T : ObjectResult
    {
        var settings = SerializationHelper.GetSettingsForEndpoint();
        return result.ConfigureFor(settings);
    }

    public static T ConfigureFor<T>(this T result, JsonSerializerSettings settings) where T : ObjectResult
    {
        // Use a custom JSON formatter to use our own serializer settings.
        result.Formatters.Clear();
        result.Formatters.Add(new NewtonsoftJsonOutputFormatter(settings, ArrayPool<char>.Shared, new MvcOptions()));

        return result;
    }
}
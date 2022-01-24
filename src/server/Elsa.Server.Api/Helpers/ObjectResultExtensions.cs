using System.Buffers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Elsa.Server.Api.Helpers;

public static class ObjectResultExtensions
{
    public static T ConfigureForWorkflowDefinition<T>(this T result) where T : ObjectResult
    {
        var settings = SerializationHelper.GetSettingsForWorkflowDefinition();

        // Use a custom JSON formatter to use our own serializer settings.
        result.Formatters.Clear();
        result.Formatters.Add(new NewtonsoftJsonOutputFormatter(settings, ArrayPool<char>.Shared, new MvcOptions()));

        return result;
    }
}
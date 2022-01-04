using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Management.Contracts;
using Elsa.Management.Serialization.Converters;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.TriggerDescriptors;

public static partial class TriggerDescriptors
{
    public static IResult ListAsync(ITriggerRegistry registry, IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        var descriptors = registry.ListAll().ToList();

        var model = new
        {
            TriggerDescriptors = descriptors
        };

        return Results.Json(model, new JsonSerializerOptions
        {
            Converters = { new TypeJsonConverter(wellKnownTypeRegistry) },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        });
    }
}
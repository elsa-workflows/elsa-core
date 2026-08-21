using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.OutputConverters.List;

/// <summary>
/// Lists output converters compatible with declared output and destination types.
/// </summary>
[PublicAPI]
internal class List(IOutputConverterRegistry registry, ISerializationTypeRegistry serializationTypeRegistry) : ElsaEndpointWithoutRequest<ListResponse<OutputConverterDescriptorModel>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/descriptors/output-converters");
        ConfigurePermissions("read:*", "read:output-converters");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var sourceTypeName = Query<string>("sourceType", false);
        var destinationTypeName = Query<string>("destinationType", false);

        if (!TryListCompatible(sourceTypeName, destinationTypeName, out var response, out var errors))
        {
            foreach (var error in errors)
                AddError(error);

            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        await Send.OkAsync(response, cancellationToken);
    }

    internal bool TryListCompatible(
        string? sourceTypeName,
        string? destinationTypeName,
        out ListResponse<OutputConverterDescriptorModel> response,
        out ICollection<string> errors)
    {
        errors = [];
        var hasSourceType = TryResolveType("sourceType", sourceTypeName, out var sourceType, out var sourceTypeError);
        var hasDestinationType = TryResolveType("destinationType", destinationTypeName, out var destinationType, out var destinationTypeError);

        if (!hasSourceType)
            errors.Add(sourceTypeError!);

        if (!hasDestinationType)
            errors.Add(destinationTypeError!);

        if (errors.Count > 0)
        {
            response = new ListResponse<OutputConverterDescriptorModel>([]);
            return false;
        }

        var descriptors = registry.FindCompatible(sourceType, destinationType).Select(Map).ToList();

        response = new ListResponse<OutputConverterDescriptorModel>(descriptors);
        return true;
    }

    private bool TryResolveType(string parameterName, string? typeName, out Type type, out string? error)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            type = null!;
            error = $"The {parameterName} query parameter is required.";
            return false;
        }

        if (SerializationTypeResolver.TryResolveType(serializationTypeRegistry, typeName, out type))
        {
            error = null;
            return true;
        }

        type = null!;
        error = $"The {parameterName} query parameter must be a registered type alias or resolvable safe type name.";
        return false;
    }

    private OutputConverterDescriptorModel Map(OutputConverterDescriptor descriptor) => new(
        descriptor.Id,
        GetTypeName(descriptor.SourceType),
        GetTypeName(descriptor.ResultType),
        descriptor.DisplayName,
        descriptor.Description,
        descriptor.SettingsSchema?.Clone());

    private string GetTypeName(Type type) => SerializationTypeResolver.TryGetAlias(serializationTypeRegistry, type, out var alias)
        ? alias
        : type.GetSimpleAssemblyQualifiedName();
}

internal record OutputConverterDescriptorModel(
    string Id,
    string SourceTypeName,
    string ResultTypeName,
    string DisplayName,
    string? Description,
    JsonElement? SettingsSchema);

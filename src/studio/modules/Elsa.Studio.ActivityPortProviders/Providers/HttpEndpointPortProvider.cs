using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the HttpEndpoint activity based on its configuration.
/// </summary>
public class HttpEndpointPortProvider : ActivityPortProviderBase
{
    private const string Done = "Done";
    private const string FileTooLarge = "File too large";
    private const string RequestTooLarge = "Request too large";
    private const string InvalidFileExtension = "Invalid file extension";
    private const string InvalidMimeType = "Invalid file MIME type";

    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) => context.ActivityDescriptor.TypeName is "Elsa.HttpEndpoint";

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        if (ExposeFileTooLargeOutcome(context.Activity)) yield return CreatePort(FileTooLarge);
        if (ExposeRequestTooLargeOutcome(context.Activity)) yield return CreatePort(RequestTooLarge);
        if (ExposeInvalidFileExtensionOutcome(context.Activity)) yield return CreatePort(InvalidFileExtension);
        if (ExposeInvalidFileMimeTypeOutcome(context.Activity)) yield return CreatePort(InvalidMimeType);
        yield return CreatePort(Done);
    }

    private bool ExposeFileTooLargeOutcome(JsonObject @case) => TryGetValue(@case, "exposeFileTooLargeOutcome", () => false);
    private bool ExposeRequestTooLargeOutcome(JsonObject @case) => TryGetValue(@case, "exposeRequestTooLargeOutcome", () => false);
    private bool ExposeInvalidFileExtensionOutcome(JsonObject @case) => TryGetValue(@case, "exposeInvalidFileExtensionOutcome", () => false);
    private bool ExposeInvalidFileMimeTypeOutcome(JsonObject @case) => TryGetValue(@case, "exposeInvalidFileMimeTypeOutcome", () => false);

    private Port CreatePort(string name)
    {
        return new Port
        {
            Name = name,
            DisplayName = name,
            Type = PortType.Flow,
        };
    }

    private static T TryGetValue<T>(JsonObject model, string propName, Func<T> defaultValue)
    {
        return model.TryGetPropertyValue(propName, out var node) ? node != null ? node.GetValue<T>() : defaultValue() : defaultValue();
    }
}
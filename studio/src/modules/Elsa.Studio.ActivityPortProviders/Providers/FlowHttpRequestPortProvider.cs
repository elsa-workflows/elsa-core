using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Converters;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;
using JetBrains.Annotations;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the FlowSendHttpRequest & DownloadHttpFile activities based on its supported status codes.
/// </summary>
[UsedImplicitly]
/// <summary>
/// Provides flow http request port services.
/// </summary>
public class FlowHttpRequestPortProvider : ActivityPortProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context)
    {
        if (context.ActivityDescriptor.TypeName is "Elsa.FlowSendHttpRequest" or "Elsa.DownloadHttpFile")
        {
            return true;
        }

        // Subclasses register under their own type name. The server descriptor already
        // carries the inherited wrapped ExpectedStatusCodes input, which SendHttpRequest
        // does not (its status-code cases are unwrapped embedded ports).
        return context.ActivityDescriptor.Inputs.Any(input =>
            input.Name == "ExpectedStatusCodes" && input.IsWrapped);
    }

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var httpPorts = GetHttpPorts(context).ToList();
        var httpPortNames = httpPorts.Select(port => port.Name).ToHashSet();

        // A subclass can declare its own outcomes (e.g. [FlowNode("Escalated")]). Keep the
        // descriptor's ports too, skipping names the HTTP ports already cover (such as "Done").
        var declaredPorts = context.ActivityDescriptor.Ports.Where(port => !httpPortNames.Contains(port.Name));

        return httpPorts.Concat(declaredPorts);
    }

    private static IEnumerable<Port> GetHttpPorts(PortProviderContext context)
    {
        var expectedStatusCodes = GetExpectedStatusCodes(context.Activity);

        foreach (var statusCode in expectedStatusCodes)
        {
            yield return new()
            {
                Name = statusCode.ToString(),
                Type = PortType.Flow,
                DisplayName = statusCode.ToString()
            };
        }
        
        yield return new()
        {
            Name = "Unmatched status code",
            Type = PortType.Flow,
            DisplayName = "Unmatched status code"
        };
        
        yield return new()
        {
            Name = "Failed to connect",
            Type = PortType.Flow,
            DisplayName = "Failed to connect"
        };
        
        yield return new()
        {
            Name = "Timeout",
            Type = PortType.Flow,
            DisplayName = "Timeout"
        };
        
        yield return new()
        {
            Name = "Done",
            Type = PortType.Flow,
            DisplayName = "Done"
        };
    }

    private static IEnumerable<int> GetExpectedStatusCodes(JsonObject activity)
    {
        var options = CreateSerializerOptions();

        var wrappedInput = activity.GetProperty<WrappedInput>(options, "expectedStatusCodes") ?? new WrappedInput
        {
            TypeName = typeof(int[]).Name,
            Expression = Expression.CreateObject(JsonSerializer.Serialize(new[] { (int)HttpStatusCode.OK }, options))
        };
        
        var objectExpression = wrappedInput.Expression;
        return JsonSerializer.Deserialize<ICollection<int>>(objectExpression.Value!.ToString()!, options)!;
    }
    
    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        options.Converters.Add(new JsonStringToIntConverter());

        return options;
    }
}
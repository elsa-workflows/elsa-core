using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;
using Humanizer;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the FlowSendHttpRequest activity based on its supported status codes.
/// </summary>
public class DynamicOutcomesPortProvider : ActivityPortProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) => GetDynamicOutcomesInput(context) != null;

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var dynamicOutcomesInputDescriptor = GetDynamicOutcomesInput(context)!;
        var dynamicOutcomes = GetDynamicOutcomes(context.Activity, dynamicOutcomesInputDescriptor).ToList();

        foreach (var dynamicOutcome in dynamicOutcomes)
        {
            yield return new Port
            {
                Name = dynamicOutcome,
                Type = PortType.Flow,
                DisplayName = dynamicOutcome
            };
        }

        var dynamicOutcomesOptions = GetDynamicOutcomeOptions(dynamicOutcomesInputDescriptor);
        var fixedOutcomes = dynamicOutcomesOptions?.FixedOutcomes;

        if (fixedOutcomes == null)
            yield break;

        foreach (var outcome in fixedOutcomes)
        {
            yield return new Port
            {
                Name = outcome,
                Type = PortType.Flow,
                DisplayName = outcome
            };
        }
    }

    private static DynamicOutcomesOptions? GetDynamicOutcomeOptions(InputDescriptor dynamicOutcomesInputDescriptor)
    {
        if (dynamicOutcomesInputDescriptor.UISpecifications?.TryGetValue(nameof(DynamicOutcomesOptions), out var dynamicOutcomesOptions) != true) 
            return default;

        var serializerOptions = CreateSerializerOptions();
        var options = ((JsonElement)dynamicOutcomesOptions!).Deserialize<DynamicOutcomesOptions>(serializerOptions);

        return options;

    }

    private static InputDescriptor? GetDynamicOutcomesInput(PortProviderContext context)
    {
        return context.ActivityDescriptor.Inputs.FirstOrDefault(x => x.UIHint == "dynamic-outcomes");
    }

    private static IEnumerable<string> GetDynamicOutcomes(JsonObject activity, PropertyDescriptor dynamicOutcomesInputDescriptor)
    {
        var options = CreateSerializerOptions();
        var dynamicOutcomesInputPropertyName = dynamicOutcomesInputDescriptor.Name.Camelize();

        var wrappedInput = activity.GetProperty<WrappedInput>(options, dynamicOutcomesInputPropertyName) ?? new WrappedInput
        {
            TypeName = dynamicOutcomesInputDescriptor.TypeName,
            Expression = Expression.CreateObject("[]")
        };

        var objectExpression = wrappedInput.Expression;
        return JsonSerializer.Deserialize<ICollection<string>>(objectExpression.Value!.ToString()!, options)!;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return options;
    }
}
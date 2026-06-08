using Elsa.AI.Abstractions.Models;
using Elsa.Workflows.Models;

namespace Elsa.AI.Host.Services;

public class ActivityGroundingMapper
{
    public ActivityGroundingSummary Map(ActivityDescriptor descriptor) =>
        new()
        {
            Type = descriptor.TypeName,
            Version = descriptor.Version,
            Namespace = descriptor.Namespace,
            Name = descriptor.Name,
            DisplayName = descriptor.DisplayName ?? descriptor.Name,
            Description = descriptor.Description,
            Category = descriptor.Category,
            IsBrowsable = descriptor.IsBrowsable,
            IsTrigger = descriptor.IsStart,
            IsContainer = descriptor.IsContainer,
            IsTerminal = descriptor.IsTerminal,
            Inputs = descriptor.Inputs.Select(MapInput).OrderBy(x => x.Name).ToList(),
            Outputs = descriptor.Outputs.Select(MapOutput).OrderBy(x => x.Name).ToList(),
            Ports = descriptor.Ports.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(),
            Constraints = GetConstraints(descriptor).ToList()
        };

    private static ActivityPortSummary MapInput(InputDescriptor input) =>
        new()
        {
            Name = input.Name,
            DisplayName = input.DisplayName ?? input.Name,
            Description = input.Description,
            Type = GetFriendlyTypeName(input.Type),
            Category = input.Category,
            IsBrowsable = input.IsBrowsable ?? true,
            IsSensitive = input.IsSensitive,
            IsRequired = input.DefaultValue == null && !IsNullable(input.Type),
            UIHint = input.UIHint,
            DefaultSyntax = input.DefaultSyntax
        };

    private static ActivityPortSummary MapOutput(OutputDescriptor output) =>
        new()
        {
            Name = output.Name,
            DisplayName = output.DisplayName ?? output.Name,
            Description = output.Description,
            Type = GetFriendlyTypeName(output.Type),
            IsBrowsable = output.IsBrowsable ?? true
        };

    private static IEnumerable<string> GetConstraints(ActivityDescriptor descriptor)
    {
        if (!descriptor.IsBrowsable)
            yield return "Not selectable from activity pickers.";

        if (descriptor.RunAsynchronously)
            yield return "Runs asynchronously by default.";

        if (descriptor.IsStart)
            yield return "Can start a workflow.";

        if (descriptor.IsTerminal)
            yield return "Can terminate a workflow path.";
    }

    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

    private static string GetFriendlyTypeName(Type? type)
    {
        if (type == null)
            return "unknown";

        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
            return $"{GetFriendlyTypeName(nullableType)}?";

        if (!type.IsGenericType)
            return type.Name;

        var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName))}>";
    }
}

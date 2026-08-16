using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.AI.Host.Tools;
using Elsa.Workflows;

namespace Elsa.AI.Host.Tools.Activities;

public class ActivityDescriptorTool(IServiceProvider serviceProvider, ActivityGroundingSearchService searchService, AIGroundingResultFormatter formatter) : GroundingToolBase
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "activities.getDescriptor",
        "Get activity descriptor",
        "Get model-safe metadata for one installed activity descriptor.",
        GroundingToolSchemas.WithProperties(
            ("type", GroundingToolSchemas.String("Activity type name or short name.")),
            ("version", GroundingToolSchemas.Integer("Optional activity version."))));

    public override ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var registry = serviceProvider.GetService(typeof(IActivityRegistry)) as IActivityRegistry;
        if (registry == null)
            return ValueTask.FromResult(formatter.Unavailable("Activity Registry"));

        var type = GetString(context.Arguments, "type");
        var version = GetInt(context.Arguments, "version");
        var descriptor = string.IsNullOrWhiteSpace(type)
            ? null
            : version != null
                ? registry.Find(type, version.Value)
                : registry.Find(x => string.Equals(x.TypeName, type, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Name, type, StringComparison.OrdinalIgnoreCase));

        if (descriptor == null)
            return ValueTask.FromResult(new AIToolResult { Status = AIToolInvocationStatus.Failed, Error = $"Activity '{type}' was not found." });

        return ValueTask.FromResult(formatter.CreateResult($"Resolved descriptor for {descriptor.TypeName} v{descriptor.Version}.", [searchService.Map(descriptor)], 1, ["ActivityRegistry"]));
    }
}

using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.AI.Host.Tools;
using Elsa.Workflows;

namespace Elsa.AI.Host.Tools.Activities;

public class ActivitiesSearchTool(IServiceProvider serviceProvider, ActivityGroundingSearchService searchService, AIGroundingResultFormatter formatter) : GroundingToolBase
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "activities.search",
        "Search activities",
        "Search installed Activity Registry descriptors by name, category, type, version, ports, and trigger behavior.",
        GroundingToolSchemas.WithProperties(
            ("query", GroundingToolSchemas.String("Free-text search term.")),
            ("category", GroundingToolSchemas.String("Activity category.")),
            ("type", GroundingToolSchemas.String("Activity type name or short name.")),
            ("version", GroundingToolSchemas.Integer("Activity version.")),
            ("input", GroundingToolSchemas.String("Input name or display name.")),
            ("output", GroundingToolSchemas.String("Output name or display name.")),
            ("trigger", GroundingToolSchemas.Boolean("Whether the activity can start a workflow."))));

    public override ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var registry = serviceProvider.GetService(typeof(IActivityRegistry)) as IActivityRegistry;
        if (registry == null)
            return ValueTask.FromResult(formatter.Unavailable("Activity Registry"));

        var descriptors = searchService.Search(
            registry,
            GetString(context.Arguments, "query"),
            GetString(context.Arguments, "category"),
            GetString(context.Arguments, "type"),
            GetInt(context.Arguments, "version"),
            GetString(context.Arguments, "input"),
            GetString(context.Arguments, "output"),
            GetBool(context.Arguments, "trigger"));
        var items = descriptors.Select(searchService.Map);

        return ValueTask.FromResult(formatter.CreateResult($"Found {descriptors.Count} installed activities.", items, descriptors.Count, ["ActivityRegistry"]));
    }
}

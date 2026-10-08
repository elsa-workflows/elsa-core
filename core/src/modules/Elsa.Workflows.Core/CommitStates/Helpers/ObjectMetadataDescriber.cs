using System.ComponentModel;
using System.Reflection;
using Humanizer;

namespace Elsa.Workflows.CommitStates;

public static class ObjectMetadataDescriber
{
    public static CommitStrategyMetadata Describe(Type strategyType)
    {
        var suffix = strategyType.IsAssignableTo(typeof(IActivityCommitStrategy)) ? "ActivityStrategy" : "WorkflowStrategy";
        var name = strategyType.Name.Replace(suffix, "");
        var displayName = strategyType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? strategyType.Name.Replace("CommitStrategy", "").Humanize();
        var description = strategyType.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

        return new()
        {
            Name = name,
            DisplayName = displayName,
            Description = description
        };
    }
}
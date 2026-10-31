using Elsa.Workflows.CommitStates;

namespace Elsa.Workflows.Api.Endpoints.CommitStrategies;

internal record CommitStrategyDescriptor(string Name, string DisplayName, string Description)
{
    public static CommitStrategyDescriptor FromStrategyMetadata(CommitStrategyMetadata metadata)
    {
        return new(metadata.Name, metadata.DisplayName, metadata.Description);
    }
    
    public static IEnumerable<CommitStrategyDescriptor> FromStrategyMetadata(IEnumerable<CommitStrategyMetadata> metadata)
    {
        return metadata.Select(FromStrategyMetadata);
    }
}
using Elsa.ActivityDefinitions.Activities;
using Elsa.ActivityDefinitions.Services;
using Elsa.Workflows.Core.Services;

namespace Elsa.ActivityDefinitions.Implementations;

/// <summary>
/// Returns a the root activity for a given <see cref="ActivityDefinitionActivity"/>.
/// </summary>
public class ActivityDefinitionPortResolver : IActivityPortResolver
{
    private readonly IActivityDefinitionStore _store;
    private readonly IActivityDefinitionMaterializer _activityDefinitionMaterializer;

    public ActivityDefinitionPortResolver(IActivityDefinitionStore store, IActivityDefinitionMaterializer activityDefinitionMaterializer)
    {
        _store = store;
        _activityDefinitionMaterializer = activityDefinitionMaterializer;
    }

    public int Priority => 0;
    public bool GetSupportsActivity(IActivity activity) => activity is ActivityDefinitionActivity;

    public async ValueTask<IEnumerable<IActivity>> GetPortsAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var root = await _activityDefinitionMaterializer.MaterializeAsync((ActivityDefinitionActivity)activity, cancellationToken);

        return new[] { root };
    }
}
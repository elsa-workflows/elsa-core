using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class MapperFactory(IActivityRegistry activityRegistry, IActivityPortService activityPortService, IActivityDisplaySettingsRegistry activityDisplaySettingsRegistry) : IMapperFactory
{
    /// <summary>
    /// Creates a flowchart mapper after ensuring activity metadata is available.
    /// </summary>
    public async Task<IFlowchartMapper> CreateFlowchartMapperAsync(CancellationToken cancellationToken = default)
    {
        var activityMapper = await CreateActivityMapperAsync(cancellationToken);
        return new FlowchartMapper(activityMapper);
    }

    /// <summary>
    /// Creates a Sequence mapper after ensuring activity metadata is available.
    /// </summary>
    public async Task<ISequenceMapper> CreateSequenceMapperAsync(CancellationToken cancellationToken = default)
    {
        var activityMapper = await CreateActivityMapperAsync(cancellationToken);
        return new SequenceMapper(activityMapper);
    }

    /// <summary>
    /// Creates an activity mapper after ensuring activity metadata is available.
    /// </summary>
    public async Task<IActivityMapper> CreateActivityMapperAsync(CancellationToken cancellationToken = default)
    {
        await activityRegistry.EnsureLoadedAsync(cancellationToken);
        return new ActivityMapper(activityRegistry, activityPortService, activityDisplaySettingsRegistry);
    }
}

namespace Elsa.Studio.Workflows.Designer.Contracts;

internal interface IMapperFactory
{
    Task<IFlowchartMapper> CreateFlowchartMapperAsync(CancellationToken cancellationToken = default);
    Task<ISequenceMapper> CreateSequenceMapperAsync(CancellationToken cancellationToken = default);
    Task<IActivityMapper> CreateActivityMapperAsync(CancellationToken cancellationToken = default);
}

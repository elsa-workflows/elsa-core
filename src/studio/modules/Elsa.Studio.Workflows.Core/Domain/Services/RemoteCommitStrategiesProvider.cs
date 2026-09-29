using Elsa.Api.Client.Resources.CommitStrategies.Contracts;
using Elsa.Api.Client.Resources.CommitStrategies.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class RemoteCommitStrategiesProvider(IBackendApiClientProvider backendApiClientProvider) : ICommitStrategiesProvider
{
    private ICollection<CommitStrategyDescriptor>? _workflowCommitStrategies;
    private ICollection<CommitStrategyDescriptor>? _activityCommitStrategies;

    /// <inheritdoc />
    public async ValueTask<IEnumerable<CommitStrategyDescriptor>> GetWorkflowCommitStrategiesAsync(CancellationToken cancellationToken = default)
    {
        if (_workflowCommitStrategies == null)
        {
            var api = await backendApiClientProvider.GetApiAsync<ICommitStrategiesApi>(cancellationToken);
            var response = await api.ListWorkflowStrategiesAsync(cancellationToken);

            _workflowCommitStrategies = response.Items;
        }

        return _workflowCommitStrategies;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<CommitStrategyDescriptor>> GetActivityCommitStrategiesAsync(CancellationToken cancellationToken = default)
    {
        if (_activityCommitStrategies == null)
        {
            var api = await backendApiClientProvider.GetApiAsync<ICommitStrategiesApi>(cancellationToken);
            var response = await api.ListActivityStrategiesAsync(cancellationToken);

            _activityCommitStrategies = response.Items;
        }

        return _activityCommitStrategies;
    }
}
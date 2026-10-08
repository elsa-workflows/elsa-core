using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Tasks;

/// <summary>
/// Updates the workflow store from <see cref="IWorkflowsProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
/// </summary>
[UsedImplicitly]
[SingleNodeTask]
public class PopulateRegistriesStartupTask(IRegistriesPopulator registriesPopulator) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await registriesPopulator.PopulateAsync(cancellationToken);
    }
}
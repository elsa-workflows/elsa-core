using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Tasks;

/// Updates the workflow store from <see cref="IWorkflowsProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
[UsedImplicitly]
[SingleNodeTask]
public class PopulateRegistriesStartupTask(IRegistriesPopulator registriesPopulator) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await registriesPopulator.PopulateAsync(cancellationToken);
    }
}
using Elsa.Abstractions;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Filters;
using JetBrains.Annotations;

namespace Elsa.Alterations.Endpoints.Alterations.Get;

/// <summary>
/// Executes an alteration plan.
/// </summary>
[PublicAPI]
public class Get : ElsaEndpointWithoutRequest<Response>
{
    private readonly IAlterationPlanStore _alterationPlanStore;
    private readonly IAlterationJobStore _alterationJobStore;

    /// <inheritdoc />
    public Get(IAlterationPlanStore alterationPlanStore, IAlterationJobStore alterationJobStore)
    {
        _alterationPlanStore = alterationPlanStore;
        _alterationJobStore = alterationJobStore;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/alterations/{id}");
        ConfigurePermissions("read:alterations");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var planId = Route<string>("id");

        // Load the plan.
        var plan = await _alterationPlanStore.FindAsync(new AlterationPlanFilter { Id = planId }, cancellationToken);

        if (plan == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        // Load the jobs.
        var jobs = (await _alterationJobStore.FindManyAsync(new AlterationJobFilter { PlanId = planId }, cancellationToken)).ToList();

        // Write response.
        var response = new Response(plan, jobs);
        await SendOkAsync(response, cancellationToken);
    }
}
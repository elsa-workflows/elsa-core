using Elsa.Abstractions;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Common;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Alterations.Endpoints.Alterations.Submit;

/// <summary>
/// Submits an alteration plan to be executed targeting workflow instances by a filter.
/// </summary>
[PublicAPI]
public class Submit : ElsaEndpoint<AlterationPlanParams, Response>
{
    private readonly IAlterationPlanScheduler _alterationPlanScheduler;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;

    /// <inheritdoc />
    public Submit(IAlterationPlanScheduler alterationPlanScheduler, IIdentityGenerator identityGenerator, ISystemClock systemClock)
    {
        _alterationPlanScheduler = alterationPlanScheduler;
        _identityGenerator = identityGenerator;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/alterations/submit");
        ConfigurePermissions("run:alterations");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(AlterationPlanParams planParams, CancellationToken cancellationToken)
    {
        // Submit the plan.
        var planId = await _alterationPlanScheduler.SubmitAsync(planParams, cancellationToken);

        // Write response.
        var response = new Response(planId);
        await SendOkAsync(response, cancellationToken);
    }
}
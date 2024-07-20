using Elsa.Abstractions;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Common.Contracts;
using Elsa.Workflows.Contracts;
using JetBrains.Annotations;

namespace Elsa.Alterations.Endpoints.Alterations.Submit;

/// <summary>
/// Executes an alteration plan.
/// </summary>
/// <inheritdoc />
[PublicAPI]
public class Submit(IAlterationPlanScheduler alterationPlanScheduler,IIdentityGenerator identityGenerator, ISystemClock systemClock) : ElsaEndpoint<AlterationPlanParams, Response>
{
    private readonly IAlterationPlanScheduler _alterationPlanScheduler = alterationPlanScheduler;
    private readonly IIdentityGenerator _identityGenerator = identityGenerator;
    private readonly ISystemClock _systemClock = systemClock;

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
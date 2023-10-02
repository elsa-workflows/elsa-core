using Elsa.Abstractions;
using Elsa.Alterations.Core.Contracts;
using Elsa.Common.Contracts;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Alterations.Endpoints.AlterationPlans.Submit;

/// <summary>
/// Executes an alteration plan.
/// </summary>
[PublicAPI]
public class Execute : ElsaEndpoint<Request, Response>
{
    private readonly IAlterationPlanScheduler _alterationPlanScheduler;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;

    /// <inheritdoc />
    public Execute(IAlterationPlanScheduler alterationPlanScheduler, IIdentityGenerator identityGenerator, ISystemClock systemClock)
    {
        _alterationPlanScheduler = alterationPlanScheduler;
        _identityGenerator = identityGenerator;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/alteration-plans/submit");
        ConfigurePermissions("execute:alteration-plans");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        // Submit the plan.
        var planId = await _alterationPlanScheduler.SubmitAsync(request.Plan, cancellationToken);

        // Write response.
        var response = new Response(planId);
        await SendOkAsync(response, cancellationToken);
    }
}
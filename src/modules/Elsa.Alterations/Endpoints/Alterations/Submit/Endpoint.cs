using Elsa.Abstractions;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Common.Contracts;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Alterations.Endpoints.Alterations.Submit;

/// <summary>
/// Executes an alteration plan.
/// </summary>
[PublicAPI]
public class Submit : ElsaEndpoint<Request, Response>
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
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        // Create a plan.
        var plan = new NewAlterationPlan
        {
            Alterations = request.Alterations.ToList(),
            WorkflowInstanceIds = request.WorkflowInstanceIds.ToList()
        };
        
        // Submit the plan.
        var planId = await _alterationPlanScheduler.SubmitAsync(plan, cancellationToken);

        // Write response.
        var response = new Response(planId);
        await SendOkAsync(response, cancellationToken);
    }
}
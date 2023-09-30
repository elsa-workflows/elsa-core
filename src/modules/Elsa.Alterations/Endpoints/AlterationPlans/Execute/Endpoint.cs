using Elsa.Abstractions;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Common.Contracts;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Alterations.Endpoints.AlterationPlans.Execute;

/// <summary>
/// Executes an alteration plan.
/// </summary>
[PublicAPI]
public class Execute : ElsaEndpoint<Request, Response>
{
    private readonly IAlterationPlanExecutor _executor;
    private readonly IAlterationPlanResultCommitter _committer;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;

    /// <inheritdoc />
    public Execute(IAlterationPlanExecutor executor, IAlterationPlanResultCommitter committer, IIdentityGenerator identityGenerator, ISystemClock systemClock)
    {
        _executor = executor;
        _committer = committer;
        _identityGenerator = identityGenerator;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/alteration-plans/execute");
        ConfigurePermissions("execute:alteration-plans");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var now = _systemClock.UtcNow;
        var plan = new AlterationPlan
        {
            Id = _identityGenerator.GenerateId(),
            Alterations = request.Alterations,
            WorkflowInstanceIds = request.WorkflowInstanceIds,
            Status = AlterationPlanStatus.Pending,
            CreatedAt = now
        };
        
        // TODO: Save plan to store.
        
        // Execute plan.
        var result = await _executor.ExecuteAsync(plan, cancellationToken);
        
        // Persist results.
        await _committer.CommitAsync(result, cancellationToken);
        
        // Write response.
        var response = new Response(result.HasSucceeded, result.Log.LogEntries.ToList());
        await SendOkAsync(response, cancellationToken);
    }
}
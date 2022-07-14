using Elsa.Services.Compensation;
using Elsa.Services.Models;

namespace Elsa.ActivityResults;

public class ConfirmResult : ActivityExecutionResult
{ 
    public ConfirmResult(string compensableActivityId)
    {
        CompensableActivityId = compensableActivityId;
    }
    
    /// <summary>
    /// The ID of a specific compensable activity to invoke.
    /// </summary>
    public string CompensableActivityId { get; }

    protected override void Execute(ActivityExecutionContext activityExecutionContext)
    {
        Confirm(activityExecutionContext);
    }
    
    private void Confirm(ActivityExecutionContext activityExecutionContext)
    {
        var compensationService = activityExecutionContext.GetService<ICompensationService>();
        compensationService.Confirm(activityExecutionContext, CompensableActivityId);
    }
}
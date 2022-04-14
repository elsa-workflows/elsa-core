using System;
using Elsa.Services.Models;

namespace Elsa.Services.Compensation;

public interface ICompensationService
{
    /// <summary>
    /// Attempts to invoke any compensation activity in the inbound trajectory of the faulting activity.
    /// </summary>
    void Compensate(ActivityExecutionContext activityExecutionContext, Exception? exception = default, string? message = default);
    
    /// <summary>
    /// Invokes the specified compensation activity.
    /// </summary>
    void Compensate(ActivityExecutionContext activityExecutionContext, string compensableActivityId, Exception? exception = default, string? message = default);
    
    /// <summary>
    /// Confirms the specified compensation activity.
    /// </summary>
    void Confirm(ActivityExecutionContext activityExecutionContext, string compensableActivityId);
}
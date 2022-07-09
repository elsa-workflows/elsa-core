using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.UserTask.Models;
using Elsa.Services.Models;

namespace Elsa.Activities.UserTask.Contracts;

/// <summary>
/// Provides access to available user actions.
/// </summary>
public interface IUserTaskService
{
    /// <summary>
    /// Returns a list of available user actions for a given workflow instance.
    /// </summary>
    Task<IEnumerable<UserAction>> GetUserActionsAsync(string workflowInstanceId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserAction>> GetAllUserActionsAsync(int? skip = default, int? take = default, string? tenantId = default, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<CollectedWorkflow>> ExecuteUserActionsAsync(TriggerUserAction taskAction, CancellationToken cancellationToken = default);

    Task<IEnumerable<CollectedWorkflow>> DispatchUserActionsAsync(TriggerUserAction taskAction, CancellationToken cancellationToken = default);
}
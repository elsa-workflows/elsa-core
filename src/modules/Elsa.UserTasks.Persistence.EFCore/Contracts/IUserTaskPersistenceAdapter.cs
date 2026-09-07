namespace Elsa.UserTasks.Persistence.EFCore.Contracts;

/// <summary>
/// Low-level record adapter used by migration/reconciliation infrastructure. The public aggregate
/// repository is implemented by the same EF repository below.
/// </summary>
public interface IUserTaskPersistenceAdapter
{
    Task<UserTaskRecord?> GetAsync(string tenantId, string taskId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserTaskRecord>> QueryAsync(
        UserTaskPersistenceQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddProjectionAsync(
        UserTaskRecord task,
        CancellationToken cancellationToken = default);

    Task<bool> TrySaveAsync(
        UserTaskRecord task,
        int expectedRevision,
        CancellationToken cancellationToken = default);
}

public sealed class UserTaskPersistenceQuery
{
    public string TenantId { get; init; } = default!;
    public IReadOnlyCollection<Elsa.UserTasks.Models.UserTaskStatus> Statuses { get; init; } = [];
    public string? AssigneeProvider { get; init; }
    public string? AssigneeType { get; init; }
    public string? AssigneeId { get; init; }
    public string? Search { get; init; }
    public int? Limit { get; init; }
}

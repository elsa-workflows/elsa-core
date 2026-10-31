namespace Elsa.Identity.Models;

/// <summary>Describes a module-owned reference that prevents deleting a user.</summary>
public sealed record UserDeletionDependency(string Source, string Description);

/// <summary>Represents the outcome of a guarded user-deletion attempt.</summary>
public abstract record UserDeletionOperationResult
{
    private UserDeletionOperationResult()
    {
    }

    public sealed record Deleted : UserDeletionOperationResult;
    public sealed record NotFound : UserDeletionOperationResult;
    public sealed record Blocked(IReadOnlyCollection<UserDeletionDependency> Dependencies) : UserDeletionOperationResult;
}

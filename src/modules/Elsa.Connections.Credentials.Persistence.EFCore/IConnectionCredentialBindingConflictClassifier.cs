using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

/// <summary>Identifies a duplicate logical-binding key insert for the configured EF provider.</summary>
public interface IConnectionCredentialBindingConflictClassifier
{
    bool IsDuplicateBindingKey(DbUpdateException exception);
}

internal sealed class NoConnectionCredentialBindingConflictClassifier : IConnectionCredentialBindingConflictClassifier
{
    public bool IsDuplicateBindingKey(DbUpdateException exception) => false;
}

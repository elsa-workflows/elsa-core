namespace Elsa.Framework.Shells;

/// <summary>
/// Represents a shell host responsible for initializing & managing shells.
/// </summary>
public interface IShellHost
{
    /// Returns the shell for the main application.
    Shell ApplicationShell { get; }
    
    /// Initializes a shell host and creates a shell for each tenant.
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// Gets a shell by tenant ID.
    Shell GetShell(string? tenantId);
}
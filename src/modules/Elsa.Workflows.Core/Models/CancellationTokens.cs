namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Provides cancellation tokens to be used by the system.
/// </summary>
public struct CancellationTokens
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancellationTokens"/> struct.
    /// </summary>
    /// <param name="applicationCancellationToken">The application cancellation token.</param>
    /// <param name="systemCancellationToken">The system cancellation token.</param>
    public CancellationTokens(CancellationToken applicationCancellationToken, CancellationToken systemCancellationToken)
    {
        ApplicationCancellationToken = applicationCancellationToken;
        SystemCancellationToken = systemCancellationToken;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CancellationTokens"/> struct.
    /// </summary>
    /// <param name="duplexCancellationToken">The duplex cancellation token.</param>
    public CancellationTokens(CancellationToken duplexCancellationToken) : this(duplexCancellationToken, duplexCancellationToken)
    {
    }
    
    /// <summary>
    /// Implicitly casts a <see cref="CancellationToken"/> to a <see cref="CancellationTokens"/> struct.
    /// </summary>
    public static implicit operator CancellationTokens(CancellationToken cancellationToken) => new(cancellationToken);

    /// <summary>
    /// Gets or sets the application cancellation token.
    /// </summary>
    public CancellationToken ApplicationCancellationToken { get; }

    /// <summary>
    /// Gets or sets the system cancellation token.
    /// </summary>
    public CancellationToken SystemCancellationToken { get; }
}
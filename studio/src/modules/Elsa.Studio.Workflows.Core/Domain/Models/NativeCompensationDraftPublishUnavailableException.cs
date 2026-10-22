namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Indicates that a stored native compensation draft cannot be published by the connected server.
/// </summary>
public sealed class NativeCompensationDraftPublishUnavailableException(string message) : InvalidOperationException(message)
{
}

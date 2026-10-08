namespace Elsa.Common.Exceptions;

/// <summary>
/// Configuration is missing.
/// </summary>
public class MissingConfigurationException : Exception
{
    /// <inheritdoc />
    public MissingConfigurationException(string message) : base(message)
    {
    }
}
namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Implementors should provide an out-stream to which a consumer can write.
/// </summary>
public interface IStandardOutStreamProvider
{
    TextWriter GetTextWriter();
}
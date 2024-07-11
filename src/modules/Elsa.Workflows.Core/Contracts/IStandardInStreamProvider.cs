namespace Elsa.Workflows.Contracts;

/// <summary>
/// Implementors should provide an in-stream from which a consumer can read.
/// </summary>
public interface IStandardInStreamProvider
{
    TextReader GetTextReader();
}
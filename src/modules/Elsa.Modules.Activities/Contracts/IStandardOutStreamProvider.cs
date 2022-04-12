namespace Elsa.Modules.Activities.Contracts;

/// <summary>
/// Implementors should provide an out-stream to which a consumer can write.
/// </summary>
public interface IStandardOutStreamProvider
{
    TextWriter GetTextWriter();
}
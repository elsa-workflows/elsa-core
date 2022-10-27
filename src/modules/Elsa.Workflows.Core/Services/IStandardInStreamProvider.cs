using System.IO;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Implementors should provide an in-stream from which a consumer can read.
/// </summary>
public interface IStandardInStreamProvider
{
    TextReader GetTextReader();
}
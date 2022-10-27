using System.IO;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class StandardOutStreamProvider : IStandardOutStreamProvider
{
    private readonly TextWriter _textWriter;

    public StandardOutStreamProvider(TextWriter textWriter)
    {
        _textWriter = textWriter;
    }

    public TextWriter GetTextWriter() => _textWriter;
}
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

public class StandardOutStreamProvider : IStandardOutStreamProvider
{
    private readonly TextWriter _textWriter;

    public StandardOutStreamProvider(TextWriter textWriter)
    {
        _textWriter = textWriter;
    }

    public TextWriter GetTextWriter() => _textWriter;
}
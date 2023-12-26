using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Services;

public class StandardOutStreamProvider : IStandardOutStreamProvider
{
    private readonly TextWriter _textWriter;

    public StandardOutStreamProvider(TextWriter textWriter)
    {
        _textWriter = textWriter;
    }

    public TextWriter GetTextWriter() => _textWriter;
}
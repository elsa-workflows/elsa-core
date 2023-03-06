using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

public class StandardInStreamProvider : IStandardInStreamProvider
{
    private readonly TextReader _textReader;
    public StandardInStreamProvider(TextReader textReader) => _textReader = textReader;
    public TextReader GetTextReader() => _textReader;
}
using Elsa.Modules.Activities.Services;

namespace Elsa.Modules.Activities.Providers;

public class StandardInStreamProvider : IStandardInStreamProvider
{
    private readonly TextReader _textReader;
    public StandardInStreamProvider(TextReader textReader) => _textReader = textReader;
    public TextReader GetTextReader() => _textReader;
}
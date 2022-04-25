using Elsa.Modules.Activities.Services;

namespace Elsa.Modules.Activities.Providers;

public class StandardOutStreamProvider : IStandardOutStreamProvider
{
    private readonly TextWriter _textWriter;

    public StandardOutStreamProvider(TextWriter textWriter)
    {
        _textWriter = textWriter;
    }

    public TextWriter GetTextWriter() => _textWriter;
}
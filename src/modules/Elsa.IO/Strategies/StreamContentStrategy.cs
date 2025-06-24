using Elsa.IO.Contracts;

namespace Elsa.IO.Strategies;

/// <summary>
/// This strategy handles content that is a <see cref="Stream"/>.
/// </summary>
public class StreamContentStrategy : IContentStrategy
{
    public bool CanHandle(object content)
    {
        return content is Stream;
    }

    public Task<Stream> ParseContentAsync(object content, CancellationToken cancellationToken)
    {
        return Task.FromResult((content as Stream)!);
    }
}
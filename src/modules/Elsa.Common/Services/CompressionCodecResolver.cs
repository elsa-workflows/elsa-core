using Elsa.Common.Codecs;

namespace Elsa.Common.Services;

/// <inheritdoc />
public class CompressionCodecResolver : ICompressionCodecResolver
{
    private readonly IDictionary<string, ICompressionCodec> _codecs;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CompressionCodecResolver"/> class.
    /// </summary>
    public CompressionCodecResolver(IEnumerable<ICompressionCodec> codecs)
    {
        _codecs = codecs.ToDictionary(c => c.GetType().Name);
    }
    
    /// <inheritdoc />
    public ICompressionCodec Resolve(string name)
    {
        return _codecs.TryGetValue(name, out var codec) ? codec : new None();
    }
}
using System.Text;

namespace Elsa.Testing.Shared;

/// <summary>
/// Forwards text to the specified list of writers.
/// </summary>
public class CombinedTextWriter : TextWriter
{
    private readonly IEnumerable<TextWriter> _targets;
    public CombinedTextWriter(IEnumerable<TextWriter> targets) => _targets = targets;
    public CombinedTextWriter(params TextWriter[] targets) => _targets = targets;
    public override Encoding Encoding => Console.Out.Encoding;

    public override void Write(char value)
    {
        foreach (var target in _targets) target.Write(value);
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var target in _targets) target.Dispose();
        base.Dispose(disposing);
    }
}
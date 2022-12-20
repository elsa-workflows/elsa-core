using System.Text;

namespace Elsa.Testing.Shared;

/// <summary>
/// Captures lines of text.
/// </summary>
public class CapturingTextWriter : TextWriter
{
    private readonly ICollection<string> _lines = new List<string>();
    private IList<char> _line = new List<char>();

    public IReadOnlyCollection<string> Lines => _lines.ToList();
    public override Encoding Encoding => Console.Out.Encoding;

    public override void Write(char value)
    {
        if (value == '\n')
        {
            FlushLine();
            _line = new List<char>();
            return;
        }

        _line.Add(value);
    }

    protected override void Dispose(bool disposing)
    {
        if (_line.Count > 0)
            FlushLine();
        base.Dispose(disposing);
    }

    private void FlushLine()
    {
        if (_line.Count > 0 && _line.Last() == '\r')
            _line.RemoveAt(_line.Count - 1);

        _lines.Add(new string(_line.ToArray()));
    }
}
using System.Text;

namespace Elsa.Python.Options;

/// <summary>
/// Options for the Python expression evaluator.
/// </summary>
public class PythonOptions
{
    /// <summary>
    /// Gets or sets the Python script files to load.
    /// </summary>
    public ICollection<string> Scripts { get; } = new List<string>();
    
    /// <summary>
    /// Appends a script to the Python engine.
    /// </summary>
    /// <param name="builder">A builder that builds the script to append.</param>
    public void AddScript(Action<StringBuilder> builder)
    {
        var sb = new StringBuilder();
        builder(sb);
        AddScript(sb.ToString());
    }

    /// <summary>
    /// Appends a script to the Python engine.
    /// </summary>
    /// <param name="script">The script to append.</param>
    public void AddScript(string script)
    {
        Scripts.Add(script);
    }
}
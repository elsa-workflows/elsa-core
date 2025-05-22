using System.Text;
using JetBrains.Annotations;
using Python.Runtime;

namespace Elsa.Expressions.Python.Options;

/// <summary>
/// Options for the Python expression evaluator.
/// </summary>
[PublicAPI]
public class PythonOptions
{
    /// <summary>
    /// Gets or sets the path to the Python DLL. Alternatively, you can set the PYTHON_DLL environment variable, which is required if you leave this property empty.
    /// </summary>
    public string? PythonDllPath { get; set; }
    
    /// <summary>
    /// Gets or sets the Python script files to load.
    /// </summary>
    public ICollection<string> Scripts { get; } = new List<string>();
    
    /// <summary>
    /// Gets or sets a list of callbacks that are invoked when the Python engine is being configured.
    /// </summary>
    public ICollection<Action<PyModule>> Scopes { get; } = new List<Action<PyModule>>();
    
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
    
    /// <summary>
    /// Registers a callback that is invoked when the Python engine is being configured.
    /// </summary>
    public void ConfigureScriptScope(Action<PyModule> configure)
    {
        Scopes.Add(configure);
    }
}
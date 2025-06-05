using System.Runtime.CompilerServices;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Server.Web;

/// <summary>
/// Executes C# code.
/// </summary>
[Activity("Elsa", "TESTS", "Tests Radio List Functionality", DisplayName = "TEST")]
public class TestRadioList : CodeActivity<object?>
{
    /// <inheritdoc />
    public TestRadioList([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public TestRadioList(string script, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(source, line)
    {
    }

    /// <summary>
    /// The script to run.
    /// </summary>
    [Input(
        Description = "Choose to download one file or entire folder",
        DefaultValue = "File",
        Options = new[] { "File", "Folder" },
        UIHint = InputUIHints.RadioList
        )]
    public Input<string> SelectedRadioOption { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        
    }
}
using Elsa.Workflows.Attributes;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Core.UnitTests;

/// <summary>
///  Write a line of text to the console.
/// </summary>
[Activity("Elsa", "Test", "Used in Testing - not a real activity")]
public class TestActivity : CodeActivity
{
    /// <summary>
    /// The text to write.
    /// </summary>
    [Description("The text to write.")]
    public Input<TestEnumType> Option { get; set; } = default!;

    [JsonConstructor]
    private TestActivity(string? source = default, int? line = default) : base(source, line)
    {
    }
    /// <inheritdoc />
    public TestActivity(Input<TestEnumType> option, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => Option = option;


}

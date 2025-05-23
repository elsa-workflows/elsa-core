using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Serialization.UIHintSerializiation;

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
    public Input<TestEnumType> Option { get; set; } = null!;

    [JsonConstructor]
    private TestActivity(string? source = null, int? line = null) : base(source, line)
    {
    }
    /// <inheritdoc />
    public TestActivity(Input<TestEnumType> option, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(source, line) => Option = option;


}

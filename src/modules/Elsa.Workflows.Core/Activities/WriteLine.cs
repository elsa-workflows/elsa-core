using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
///  Write a line of text to the console.
/// </summary>
[Activity("Elsa", "Console", "Write a line of text to the console.")]
public class WriteLine : CodeActivity
{
    /// <inheritdoc />
    internal WriteLine([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    [JsonConstructor]
    public WriteLine() : this(default, default)
    {
    }

    /// <inheritdoc />
    public WriteLine(string text, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Literal<string>(text), source, line)
    {
    }

    /// <inheritdoc />
    public WriteLine(Func<string> text, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(new DelegateBlockReference<string>(text), source, line)
    {
    }

    /// <inheritdoc />
    public WriteLine(Func<ExpressionExecutionContext, string?> text, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(new DelegateBlockReference<string?>(text), source, line)
    {
    }

    /// <inheritdoc />
    public WriteLine(Variable<string> variable, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => Text = new Input<string>(variable);

    /// <inheritdoc />
    public WriteLine(Literal<string> literal, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => Text = new Input<string>(literal);

    /// <inheritdoc />
    public WriteLine(DelegateBlockReference delegateBlockExpression, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => Text = new Input<string>(delegateBlockExpression);

    /// <inheritdoc />
    public WriteLine(Input<string> text, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => Text = text;
        
    /// <summary>
    /// The text to write.
    /// </summary>
    [Description("The text to write.")]
    public Input<string> Text { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var text = context.Get(Text);
        var provider = context.GetService<IStandardOutStreamProvider>() ?? new StandardOutStreamProvider(System.Console.Out);
        var textWriter = provider.GetTextWriter();
        textWriter.WriteLine(text);
    }
}
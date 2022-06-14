using System;
using System.Collections.Generic;
using System.Reflection;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.ActivityInputOptions;
using Elsa.Workflows.Management.Implementations;
using Elsa.Workflows.Management.Models;

namespace Elsa.Samples.Web1.Activities;

[Activity("Samples")]
public class WriteLines : Activity, IActivityPropertyOptionsProvider
{
    public WriteLines()
    {
    }

    public WriteLines(ICollection<string> lines) : this(new Literal<ICollection<string>>(lines))
    {
    }

    public WriteLines(Func<ICollection<string>> lines) : this(new DelegateBlockReference<ICollection<string>>(lines))
    {
    }

    public WriteLines(Func<ExpressionExecutionContext, ICollection<string>?> text) : this(new DelegateBlockReference<ICollection<string>?>(text))
    {
    }

    public WriteLines(Variable<ICollection<string>> variable) => Lines = new Input<ICollection<string>>(variable);
    public WriteLines(Literal<ICollection<string>> literal) => Lines = new Input<ICollection<string>>(literal);
    public WriteLines(DelegateBlockReference delegateBlockExpression) => Lines = new Input<ICollection<string>>(delegateBlockExpression);
    public WriteLines(Input<ICollection<string>> lines) => Lines = lines;
    public Input<ICollection<string>> Lines { get; set; } = default!;

    [Input(Options = new[] { "Normal", "Bold", "Italic" }, UIHint = InputUIHints.RadioList)]
    public Input<string> Format { get; set; } = default!;

    [Input(OptionsProvider = typeof(WriteLines), UIHint = InputUIHints.MultiLine)]
    public Input<string> Body { get; set; } = default!;

    [Input(UIHint = InputUIHints.CodeEditor, OptionsProvider = typeof(WriteLines))]
    public Input<string> Script { get; set; } = default!;

    [Input(UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> Tags { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var lines = context.Get(Lines)!;

        foreach (var line in lines)
            Console.WriteLine(line);
    }

    public object? GetOptions(PropertyInfo property)
    {
        return property.Name switch
        {
            nameof(Body) => new { EditorHeight = EditorHeight.Large },
            nameof(Script) => new { EditorHeight = EditorHeight.Large, Language = "JavaScript" },
            _ => null
        };
    }
}
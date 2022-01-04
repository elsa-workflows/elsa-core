using System;
using System.Collections.Generic;
using System.ComponentModel;
using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Samples.Web1.Activities;

[Activity("Samples.WriteLines")]
[Category("Samples")]
public class WriteLines : Activity
{
    public WriteLines()
    {
    }

    public WriteLines(ICollection<string> lines) : this(new Literal<ICollection<string>>(lines))
    {
    }

    public WriteLines(Func<ICollection<string>> lines) : this(new DelegateReference<ICollection<string>>(lines))
    {
    }

    public WriteLines(Func<ExpressionExecutionContext, ICollection<string>?> text) : this(new DelegateReference<ICollection<string>?>(text))
    {
    }

    public WriteLines(Variable<ICollection<string>> variable) => Lines = new Input<ICollection<string>>(variable);
    public WriteLines(Literal<ICollection<string>> literal) => Lines = new Input<ICollection<string>>(literal);
    public WriteLines(DelegateReference delegateExpression) => Lines = new Input<ICollection<string>>(delegateExpression);
    public WriteLines(Input<ICollection<string>> lines) => Lines = lines;
    public Input<ICollection<string>> Lines { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var lines = context.Get(Lines)!;

        foreach (var line in lines) 
            Console.WriteLine(line);
    }
}
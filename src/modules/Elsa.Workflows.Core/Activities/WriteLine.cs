using System.ComponentModel;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Implementations;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

[Activity("Elsa", "Console", "Write a line of text to the console.")]
public class WriteLine : Activity
{
    public WriteLine()
    {
    }
        
    public WriteLine(string text) : this(new Literal<string>(text))
    {
    }
        
    public WriteLine(Func<string> text) : this(new DelegateBlockReference<string>(text))
    {
    }
        
    public WriteLine(Func<ExpressionExecutionContext, string?> text) : this(new DelegateBlockReference<string?>(text))
    {
    }

    public WriteLine(Variable<string> variable) => Text = new Input<string>(variable);
    public WriteLine(Literal<string> literal) => Text = new Input<string>(literal);
    public WriteLine(DelegateBlockReference delegateBlockExpression) => Text = new Input<string>(delegateBlockExpression);
    public WriteLine(Input<string> text) => Text = text;
        
    [Description("The text to write.")]
    public Input<string> Text { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var text = context.Get(Text);
        var provider = context.GetService<IStandardOutStreamProvider>() ?? new StandardOutStreamProvider(System.Console.Out);
        var textWriter = provider.GetTextWriter();
        textWriter.WriteLine(text);
    }
}
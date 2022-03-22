using System.ComponentModel;
using Elsa.Models;

namespace Elsa.Activities.Console;

public class WriteLine : Activity
{
    public WriteLine()
    {
    }
        
    public WriteLine(string text) : this(new Literal<string>(text))
    {
    }
        
    public WriteLine(Func<string> text) : this(new DelegateReference<string>(text))
    {
    }
        
    public WriteLine(Func<ExpressionExecutionContext, string?> text) : this(new DelegateReference<string?>(text))
    {
    }

    public WriteLine(Variable<string> variable) => Text = new Input<string>(variable);
    public WriteLine(Literal<string> literal) => Text = new Input<string>(literal);
    public WriteLine(DelegateReference delegateExpression) => Text = new Input<string>(delegateExpression);
    public WriteLine(Input<string> text) => Text = text;
        
    [Description("The text to write.")]
    public Input<string> Text { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var text = context.Get(Text);
        System.Console.WriteLine(text);
    }
}
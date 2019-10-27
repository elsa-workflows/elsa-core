using Elsa.Activities.Console.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample13
{
    public class ActivityOutputWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(x => x.TextExpression = new LiteralExpression("Hi! What's your name?"))
                .Then<ReadLine>().WithName("Name")
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`${Name.Input}, that's a great name! Now, what's your age?`"))
                .Then<ReadLine>().WithName("Age")
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`I see! So you were born in ${getDateOfBirth(parseInt(Age.Input))}. What a year to be alive!`"));
        }
    }
}
using Elsa.Activities.Console.Activities;
using Elsa.Expressions;
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
                .Then<ReadLine>(id: "name")
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`${name.Input}, that's a great name! Now, what's your age?`"))
                .Then<ReadLine>(id: "age")
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`I see! So you were born in ${getDateOfBirth(parseInt(age.Input))}. What a year to be alive!`"));
        }
    }
}
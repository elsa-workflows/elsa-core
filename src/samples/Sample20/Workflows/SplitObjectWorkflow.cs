using System.Collections;
using Elsa;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Activities.Reflection.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Scripting.Liquid;
using Elsa.Services;
using Elsa.Services.Models;
using Sample20.Models;

namespace Sample20.Workflows
{
    public class SplitObjectWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(x => x.TextExpression = new LiteralExpression("Running Split Object Workflow."))
                .Then<SetVariable>(x =>
                {
                    x.VariableName = "Customer1";
                    x.ValueExpression = new JavaScriptExpression<Customer>("({ Name: 'Johnny', Age: 21, Cash: 10 })");
                })
                .Then<SetVariable>(x =>
                {
                    x.VariableName = "Customer2";
                    x.ValueExpression = new JavaScriptExpression<Customer>("({ Name: 'Margaret', Age: 19, Cash: 20 })");
                })
                .Then<SetVariable>(x =>
                {
                    x.VariableName = "Customer3";
                    x.ValueExpression = new JavaScriptExpression<Customer>("({ Name: 'Bill', Age: 25, Cash: 50 })");
                })
                .Then<SetVariable>(x =>
                {
                    x.VariableName = "SomePrimitive";
                    x.ValueExpression = new JavaScriptExpression<string>("'Test'");
                })
                .Then<ForEach>(
                    x => x.CollectionExpression = new JavaScriptExpression<IList>("[Customer1, Customer2, Customer3]"),
                    forEach =>
                    {
                        forEach
                            .When(OutcomeNames.Iterate)
                            .Then<SplitObject>(x =>
                            {
                                x.Object = new JavaScriptExpression<object>("CurrentValue");
                                x.Properties = new[] {nameof(Customer.Name), nameof(Customer.Age), nameof(Customer.Cash)};
                            }, splitObject =>
                            {
                                splitObject
                                    .When(nameof(Customer.Name))
                                    .Then<WriteLine>(x => x.TextExpression = new LiquidExpression<string>("Processing name: {{ Variables.Name }}."))
                                    .Then("Join");
                                
                                splitObject
                                    .When(nameof(Customer.Age))
                                    .Then<WriteLine>(x => x.TextExpression = new LiquidExpression<string>("Processing age: {{ Variables.Age }}."))
                                    .Then("Join");
                                
                                splitObject
                                    .When(nameof(Customer.Cash))
                                    .Then<WriteLine>(x => x.TextExpression = new LiquidExpression<string>("Processing cash: {{ Variables.Cash }}."))
                                    .Then("Join");
                            })
                            .Add<Join>(x => x.Mode = Join.JoinMode.WaitAll).WithName("Join")
                            .Then(forEach);
                    })
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("All set."));
        }
    }
}
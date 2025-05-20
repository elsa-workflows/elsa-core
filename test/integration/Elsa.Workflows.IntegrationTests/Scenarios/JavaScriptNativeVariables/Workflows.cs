using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Models;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JavaScriptNativeVariables;

public class JavaScriptWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(Guid.NewGuid().ToString());
        var customerVariable = workflow.WithVariable("customer", Data.CreateCustomerModel());
        
        workflow.Root = new Sequence
        {
            Activities =
            {
                new RunJavaScript
                {
                    Script = new("""
                                 const customer = variables.customer;
                                 customer.Name = 'Jane Doe';
                                 customer.Orders.sort((a, b) => a.Product.localeCompare(b.Product));
                                 variables.customer = {...customer, Email: 'jane.doe@acme.com'};
                                 """)
                },
                new WriteLine(JavaScriptExpression.Create("variables.customer.Name")),
                new WriteLine(JavaScriptExpression.Create("variables.customer.Orders.map(x => x.Product).join(', ')")),
                new WriteLine(JavaScriptExpression.Create("variables.customer.Email"))
            }
        };
    }
}
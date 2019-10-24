using Elsa.Activities.Console.Activities;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Sample17.Activities;
using Sample17.Models;

namespace Sample17
{
    public class CreatePersonWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter name:"))
                .Then<ReadLine>().WithId("NameInput")
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter age:"))
                .Then<ReadLine>().WithId("AgeInput")
                .Then<CreatePerson>(
                    x =>
                    {
                        x.TitleExpression = new JavaScriptExpression<string>("NameInput.Input");
                        x.AgeExpression = new JavaScriptExpression<int>("AgeInput.Input");
                    }).WithId("CreatePerson")
                .Then<SetVariable>(
                    x =>
                    {
                        x.VariableName = "Person";
                        x.ValueExpression = new JavaScriptExpression<Person>("CreatePerson.Person");
                    })
                .Then<SetVariable>(
                    x =>
                    {
                        x.VariableName = "Age";
                        x.ValueExpression = new JavaScriptExpression<int>("CreatePerson.Person.Age");
                    })
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`A new person was created with name \"${Person.FullName}\" and age \"${Age}\"`"));
        }
    }
}
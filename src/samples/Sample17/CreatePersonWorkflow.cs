using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
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
                .StartWith<WriteLine>(x => x.Text = new LiteralExpression<string>("Enter name:"))
                .Then<ReadLine>().WithName("NameInput")
                .Then<WriteLine>(x => x.Text = new LiteralExpression<string>("Enter age:"))
                .Then<ReadLine>().WithName("AgeInput")
                .Then<CreatePerson>(
                    x =>
                    {
                        x.TitleScriptExpression = new JavaScriptExpression<string>("NameInput.Input");
                        x.AgeScriptExpression = new JavaScriptExpression<int>("AgeInput.Input");
                    }).WithName("CreatePerson")
                .Then<SetVariable>(
                    x =>
                    {
                        x.VariableName = "Person";
                        x.Value = new JavaScriptExpression<Person>("CreatePerson.Person");
                    })
                .Then<SetVariable>(
                    x =>
                    {
                        x.VariableName = "Age";
                        x.Value = new JavaScriptExpression<int>("CreatePerson.Person.Age");
                    })
                .Then<WriteLine>(x => x.Text = new JavaScriptExpression<string>("`A new person was created with name \"${Person.FullName}\" and age \"${CreatePerson.Age}\"`"));
        }
    }
}
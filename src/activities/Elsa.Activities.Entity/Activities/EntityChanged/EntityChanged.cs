using Elsa.Activities.Entity.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Entity
{
    [Trigger(
       Category = "Entity",
       DisplayName = "Entity Changed",
       Description = "Trigger when Entity Changed",
       Outcomes = new[] { OutcomeNames.Done }
   )]
    public class EntityChanged : Activity
    {
        [ActivityProperty(Type = ActivityPropertyTypes.Text, Hint = "Entity Name")]
        public string? EntityName { get; set; }

        [ActivityProperty(Type = ActivityPropertyTypes.Text, Hint = "Action")]
        [SelectOptions("Added", "Updated", "Deleted")]
        public EntityChangedAction? Action { get; set; }

        protected override IActivityExecutionResult OnExecute() => Done(new EntityChangedModel(EntityName, Action));

    }
}

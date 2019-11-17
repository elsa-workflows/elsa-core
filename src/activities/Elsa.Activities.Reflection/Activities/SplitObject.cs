using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Reflection.Activities
{
    /// <summary>
    /// Split object in multiple parts to process separately.
    /// </summary>
    [ActivityDefinition(
        Category = "Reflection",
        Description = "Split object in multiple parts to process separately.",
        RuntimeDescription = "Split object in multiple parts to process separately.",
        Outcomes = "x => x.state.properties.map(c => c.toString())"
    )]
    public class SplitObject : Activity
    {
        public SplitObject()
        {
            Properties = new List<string>();
        }

        [ActivityProperty(Hint = "Enter an expression that evaluates to the object to split.")]
        public WorkflowExpression<object> Object
        {
            get => GetState<WorkflowExpression<object>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "A comma-separated list of possible outcomes for the split parts. These are the property names of the split object.")]
        public IReadOnlyCollection<string> Properties
        {
            get => GetState<IReadOnlyCollection<string>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var splitObject = await context.EvaluateAsync(Object, cancellationToken);
            
            if (splitObject != null)
            {
                foreach (var property in Properties)
                {
                    var propValue = FollowPropertyPath(splitObject, property);
                    context.SetVariable(property, propValue);
                }
            }

            return Outcomes(Properties.ToList());
        }

        private object FollowPropertyPath(object value, string path)
        {
            var currentType = value.GetType();

            foreach (var propertyName in path.Split('.'))
            {
                var property = currentType.GetProperty(propertyName);
                value = property.GetValue(value, null);
                currentType = property.PropertyType;
            }
            return value;
        }

    }
}
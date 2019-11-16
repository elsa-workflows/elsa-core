using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Reflection.Activities
{
    /// <summary>
    /// Execute a Method by reflection.
    /// </summary>
    [ActivityDefinition(
        Category = "Reflection",
        Description = "Split object in multiple parts to process seperately.",
        RuntimeDescription = "Split object in multiple parts to process seperately.",
        Outcomes = "x => x.state.properties.map(c => c.toString())"
    )]
    public class SplitObject : Activity
    {
        public SplitObject()
        {
            Properties = new List<string>();
        }

        [ActivityProperty(Hint = "The variable with the object to split")]
        public string InputVariableName
        {
            get => GetState<string>(null, "InputVariableName");
            set => SetState(value, "InputVariableName");
        }

        [ActivityProperty(Hint = "A comma-separated list of possible outcomes for the split parts. These are the property names of the splitted object.")]
        public IReadOnlyCollection<string> Properties
        {
            get => GetState<IReadOnlyCollection<string>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            object splitObject = null;
            if (!string.IsNullOrEmpty(InputVariableName))
            {
                splitObject = context.GetVariable<object>(InputVariableName);
            }

            if (splitObject != null)
            {
                foreach (var Property in Properties)
                {
                    object PropValue = FollowPropertyPath(splitObject, Property);
                    context.SetVariable(Property, PropValue);
                }
            }

            return Outcomes(Properties.ToList());
        }

        protected object FollowPropertyPath(object value, string path)
        {
            Type currentType = value.GetType();

            foreach (string propertyName in path.Split('.'))
            {
                PropertyInfo property = currentType.GetProperty(propertyName);
                value = property.GetValue(value, null);
                currentType = property.PropertyType;
            }
            return value;
        }

    }
}
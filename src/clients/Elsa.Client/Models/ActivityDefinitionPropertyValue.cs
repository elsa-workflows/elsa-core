using System;

namespace Elsa.Client.Models
{
    public class ActivityDefinitionPropertyValue
    {
        public static ActivityDefinitionPropertyValue Literal<T>(T value) => new ActivityDefinitionPropertyValue(value!.ToString(), "Literal", typeof(T));
        public static ActivityDefinitionPropertyValue Liquid<T>(string expression) => new ActivityDefinitionPropertyValue(expression, "Liquid", typeof(T));
        public static ActivityDefinitionPropertyValue JavaScript<T>(string expression) => new ActivityDefinitionPropertyValue(expression, "JavaScript", typeof(T));

        public ActivityDefinitionPropertyValue()
        {
        }

        public ActivityDefinitionPropertyValue(string expression, string syntax, Type type)
        {
            Expression = expression;
            Syntax = syntax;
            Type = type;
        }

        public Type Type { get; set; } = default!;
        public string Syntax { get; set; } = default!;
        public string Expression { get; set; } = default!;
    }
}
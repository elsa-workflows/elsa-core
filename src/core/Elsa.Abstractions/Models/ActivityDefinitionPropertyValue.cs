using System;
using Elsa.Serialization.Converters;
using Newtonsoft.Json;

namespace Elsa.Models
{
    public class ActivityDefinitionPropertyValue
    {
        public static ActivityDefinitionPropertyValue Literal<T>(T value) => new ActivityDefinitionPropertyValue(value!.ToString(), "Literal");
        public static ActivityDefinitionPropertyValue Liquid<T>(string expression) => new ActivityDefinitionPropertyValue(expression, "Liquid");
        public static ActivityDefinitionPropertyValue JavaScript<T>(string expression) => new ActivityDefinitionPropertyValue(expression, "JavaScript");

        public ActivityDefinitionPropertyValue()
        {
        }

        public ActivityDefinitionPropertyValue(string expression, string syntax)
        {
            Expression = expression;
            Syntax = syntax;
            //Type = type;
        }

        //[JsonConverter(typeof(TypeJsonConverter))] public Type Type { get; set; } = default!;
        public string Syntax { get; set; } = default!;
        public string Expression { get; set; } = default!;
    }
}
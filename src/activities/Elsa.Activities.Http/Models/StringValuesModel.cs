using Microsoft.Extensions.Primitives;

namespace Elsa.Activities.Http.Models
{
    public class StringValuesModel
    {
        public string? Value { get; set; }
        public string[]? Values { get; set; }

        public StringValuesModel()
        {
        }

        public StringValuesModel(StringValues value)
        {
            Value = value.Count == 1 ? value.ToString() : default;
            Values = value.Count != 1 ? value.ToArray() : default;
        }

        public override string? ToString()
        {
            if (Values == null)
                return Value;

            return Values.Length switch
            {
                0 => default,
                1 => Values[0],
                _ => string.Join(",", Values)
            };
        }
    }
}
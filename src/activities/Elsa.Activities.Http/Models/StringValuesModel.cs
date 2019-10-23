using Microsoft.Extensions.Primitives;

namespace Elsa.Activities.Http.Models
{
    public class StringValuesModel
    {
        public string Value { get; set; }
        public string[] Values { get; set; }

        public StringValuesModel()
        {
        }

        public StringValuesModel(StringValues value)
        {
            Value = value.Count == 1 ? value.ToString() : default;
            Values = value.Count != 1 ? value.ToArray() : default;
        }

        public override string ToString()
        {
            if (Values == null)
                return Value;

            switch (Values.Length)
            {
                case 0:
                    return null;
                case 1:
                    return Values[0];
                default:
                    return string.Join(",", Values);
            }
        }
    }
}
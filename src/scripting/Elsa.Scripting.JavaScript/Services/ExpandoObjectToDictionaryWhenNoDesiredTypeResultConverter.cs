using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;

namespace Elsa.Scripting.JavaScript.Services
{
    public class ExpandoObjectToDictionaryWhenNoDesiredTypeResultConverter : IConvertsJintEvaluationResult
    {
        private readonly IConvertsJintEvaluationResult wrapped;
        private readonly IConvertsEnumerableToObject enumerableConverter;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if(evaluationResult is ExpandoObject expando && desiredType == typeof(object))
                return RecursivelyPrepareExpandoObjectForReturn(expando);

            return wrapped.ConvertToDesiredType(evaluationResult, desiredType);
        }

        private object? RecursivelyPrepareExpandoObjectForReturn(ExpandoObject obj)
        {
            IDictionary<string,object?> ExpandoToDictionary(ExpandoObject expando)
            {
                var json = JsonConvert.SerializeObject(expando);
                return (IDictionary<string,object?>) JsonConvert.DeserializeObject(json, typeof(Dictionary<string,object?>))!;
            }

            var expandoDictionary = obj as IDictionary<string,object>;

            var allValuesToReplace = (from kvp in expandoDictionary
                                      where kvp.Value is IEnumerable && !(kvp.Value is string)
                                      let val = (IEnumerable) kvp.Value
                                      let replacementValue = (val is ExpandoObject expando)
                                            ? RecursivelyPrepareExpandoObjectForReturn(expando)
                                            : enumerableConverter.ConvertEnumerable(val)
                                      select new { Key = kvp.Key, Value = replacementValue })
                .ToDictionary(k => k.Key, v => v.Value);

            var output = ExpandoToDictionary(obj);
            foreach(var kvp in allValuesToReplace)
                output[kvp.Key] = kvp.Value;

            return output;
        }

        public ExpandoObjectToDictionaryWhenNoDesiredTypeResultConverter(IConvertsEnumerableToObject enumerableConverter, IConvertsJintEvaluationResult wrapped)
        {
            this.wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
            this.enumerableConverter = enumerableConverter ?? throw new ArgumentNullException(nameof(enumerableConverter));
        }
    }
}
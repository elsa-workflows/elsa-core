using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Elsa.Serialization;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Scripting.JavaScript.Services
{
    public class ExpandoObjectToDictionaryWhenNoDesiredTypeResultConverter : IConvertsJintEvaluationResult
    {
        private readonly IConvertsJintEvaluationResult _wrapped;
        private readonly IConvertsEnumerableToObject _enumerableConverter;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if(evaluationResult is ExpandoObject expando)
            {
                if(desiredType == typeof(object))
                    return RecursivelyPrepareExpandoObjectForReturn(expando);

                var serializationSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                var json = JsonConvert.SerializeObject(evaluationResult, desiredType, serializationSettings);
                return JsonConvert.DeserializeObject(json, desiredType, serializationSettings);
            }

            return _wrapped.ConvertToDesiredType(evaluationResult, desiredType);
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
                                            : _enumerableConverter.ConvertEnumerable(val)
                                      select new { Key = kvp.Key, Value = replacementValue })
                .ToDictionary(k => k.Key, v => v.Value);

            var output = ExpandoToDictionary(obj);
            foreach(var kvp in allValuesToReplace)
                output[kvp.Key] = kvp.Value;

            return output;
        }

        public ExpandoObjectToDictionaryWhenNoDesiredTypeResultConverter(IConvertsEnumerableToObject enumerableConverter, IConvertsJintEvaluationResult wrapped)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
            _enumerableConverter = enumerableConverter ?? throw new ArgumentNullException(nameof(enumerableConverter));
        }
    }
}
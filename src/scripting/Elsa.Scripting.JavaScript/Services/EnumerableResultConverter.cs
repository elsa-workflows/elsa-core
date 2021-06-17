using System;
using System.Collections;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Scripting.JavaScript.Services
{
    public class EnumerableResultConverter : IConvertsJintEvaluationResult, IConvertsEnumerableToObject
    {
        private readonly IConvertsJintEvaluationResult? _wrapped;
        
        public EnumerableResultConverter(IConvertsJintEvaluationResult? wrapped) => _wrapped = wrapped;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if (evaluationResult is IEnumerable enumerable && !(evaluationResult is ExpandoObject))
                return ConvertEnumerable(enumerable, desiredType);

            return _wrapped?.ConvertToDesiredType(evaluationResult, desiredType);
        }

        private static object? ConvertEnumerable(IEnumerable enumerable, Type? desiredType = null)
        {
            if (enumerable is string) return enumerable;
            if (enumerable is JObject) return enumerable;
            if (enumerable is byte[]) return enumerable;

            var destinationType = GetDestinationType(desiredType);
            var json = JsonConvert.SerializeObject(enumerable);
            return JsonConvert.DeserializeObject(json, destinationType);
        }

        private static Type GetDestinationType(Type? desiredType) =>
            desiredType switch
            {
                { } t when t == typeof(object) => typeof(object[]),
                null => typeof(object[]),
                _ => desiredType,
            };

        object? IConvertsEnumerableToObject.ConvertEnumerable(IEnumerable enumerable) => ConvertEnumerable(enumerable);
    }
}
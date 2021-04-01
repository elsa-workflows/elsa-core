using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;

namespace Elsa.Scripting.JavaScript.Services
{
    public class EnumerableResultConverter : IConvertsJintEvaluationResult, IConvertsEnumerableToObject
    {
        readonly IConvertsJintEvaluationResult? wrapped;

        public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
        {
            if(evaluationResult is IEnumerable enumerable && !(evaluationResult is ExpandoObject))
                return ConvertEnumerable(enumerable, desiredType);
            
            return wrapped?.ConvertToDesiredType(evaluationResult, desiredType);
        }

        object? ConvertEnumerable(IEnumerable enumerable, Type? desiredType = null)
        {
            if(enumerable is string) return enumerable;

            var destinationType = GetDestinationType(desiredType);
            var json = JsonConvert.SerializeObject(enumerable);
            return JsonConvert.DeserializeObject(json, destinationType);
        }

        static Type GetDestinationType(Type? desiredType)
        {
            return desiredType switch
            {
                Type t when t == typeof(object)     => typeof(object[]),
                null                                => typeof(object[]),
                _                                   => desiredType,
            };
        }

        object? IConvertsEnumerableToObject.ConvertEnumerable(IEnumerable enumerable)
            => ConvertEnumerable(enumerable);

        public EnumerableResultConverter(IConvertsJintEvaluationResult? wrapped)
        {
            this.wrapped = wrapped;
        }
    }
}
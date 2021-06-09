// using System;
// using Newtonsoft.Json.Linq;
//
// namespace Elsa.Scripting.JavaScript.Services
// {
//     public class JObjectResultConverter : IConvertsJintEvaluationResult
//     {
//         readonly IConvertsJintEvaluationResult _wrapped;
//         
//         public JObjectResultConverter(IConvertsJintEvaluationResult wrapped) => _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
//
//         public object? ConvertToDesiredType(object? evaluationResult, Type desiredType)
//         {
//             if(evaluationResult is JObject jObject)
//                 return JObjectExtensions.DeserializeState(jObject, desiredType);
//
//             return _wrapped.ConvertToDesiredType(evaluationResult, desiredType);
//         }
//     }
// }
// using Elsa.Workflows.Core;
// using Elsa.Workflows.Core.Helpers;
// using Elsa.Workflows.Core.Services;
// using Elsa.Workflows.Runtime.Stimuli;
//
// namespace Elsa.Workflows.Runtime.Models;
//
// public static class Stimulus
// {
//     public static StandardStimulus Standard<T>(string? hash = default, IDictionary<string, object>? input = default, string? correlationId = default) where T : IActivity =>
//         new(ActivityTypeNameHelper.GenerateTypeName<T>(), hash, input, correlationId);
//
//     public static StandardStimulus Standard<T>(string? hash, object input, string? correlationId = default) where T : IActivity =>
//         new(ActivityTypeNameHelper.GenerateTypeName<T>(), hash, input.ToDictionary(), correlationId);
//
//     public static StandardStimulus Standard<T>(object input, string? correlationId = default) where T : IActivity =>
//         new(ActivityTypeNameHelper.GenerateTypeName<T>(), default, input.ToDictionary(), correlationId);
//
//     public static StandardStimulus Standard(string activityTypeName, string? hash = default, IDictionary<string, object>? input = default, string? correlationId = default) => new(activityTypeName, hash, input, correlationId);
//     public static StandardStimulus Standard(string activityTypeName, string? hash, object? input, string? correlationId = default) => new(activityTypeName, hash, input?.ToDictionary(), correlationId);
//     public static StandardStimulus Standard(string activityTypeName, object? input, string? correlationId = default) => new(activityTypeName, default, input?.ToDictionary(), correlationId);
// }
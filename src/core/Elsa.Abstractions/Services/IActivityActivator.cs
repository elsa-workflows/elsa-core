// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Models;
// using Elsa.Services.Models;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Elsa.Services
// {
//     public interface IActivityActivator
//     {
//         Task<IActivity> ActivateActivity(IServiceScope serviceScope, string activityTypeName, Action<IActivity>? setup = default, CancellationToken cancellationToken = default);
//         IActivity ActivateActivity(IServiceScope serviceScope, IActivityBlueprint activityBlueprint);
//         T ActivateActivity<T>(IServiceScope serviceScope, Action<T>? configure = default) where T : class, IActivity;
//         IActivity ActivateActivity(IServiceScope serviceScope, Type type);
//         IActivity ActivateActivity(IServiceScope serviceScope, ActivityDefinition activityDefinition);
//         IEnumerable<Type> GetActivityTypes();
//         Type? GetActivityType(string activityTypeName);
//     }
// }
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.ActivityProviders;
// using Elsa.Models;
// using Elsa.Services.Models;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Elsa.Services
// {
//     public class ActivityActivator : IActivityActivator
//     {
//         private readonly IActivityTypeService _activityTypeService;
//
//         public ActivityActivator(IActivityTypeService activityTypeService)
//         {
//             _activityTypeService = activityTypeService;
//         }
//
//         public async Task<IActivity> ActivateActivity(IServiceScope serviceScope, string activityTypeName, Action<IActivity>? setup, CancellationToken cancellationToken)
//         {
//             var activityType = await _activityTypeService.GetActivityTypeAsync(activityTypeName, cancellationToken);
//             var activity = activityType.
//
//             setup?.Invoke(activity);
//             return activity;
//         }
//         
//         public T ActivateActivity<T>(IServiceScope serviceScope, Action<T>? setup = null) where T : class, IActivity
//         {
//             var activity = ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceScope.ServiceProvider);
//             setup?.Invoke(activity);
//             return activity;
//         }
//
//         public IActivity ActivateActivity(IServiceScope serviceScope, Type type) => (IActivity)ActivatorUtilities.GetServiceOrCreateInstance(serviceScope.ServiceProvider, type);
//
//         public IActivity ActivateActivity(IServiceScope serviceScope, IActivityBlueprint activityBlueprint)
//         {
//             return ActivateActivity(
//                 serviceScope,
//                 activityBlueprint.Type,
//                 activity =>
//                 {
//                     activity.Id = activityBlueprint.Id;
//                     activity.Name = activityBlueprint.Name;
//                     activity.PersistWorkflow = activityBlueprint.PersistWorkflow;
//                     activity.SaveWorkflowContext = activityBlueprint.SaveWorkflowContext;
//                     activity.LoadWorkflowContext = activityBlueprint.LoadWorkflowContext;
//                 });
//         }
//
//         public IActivity ActivateActivity(IServiceScope serviceScope, ActivityDefinition activityDefinition)
//         {
//             var activity = ActivateActivity(serviceScope, activityDefinition.Type);
//             activity.Description = activityDefinition.Description;
//             activity.Id = activityDefinition.ActivityId;
//             activity.Name = activityDefinition.Name;
//             activity.DisplayName = activityDefinition.DisplayName;
//             activity.PersistWorkflow = activityDefinition.PersistWorkflow;
//             activity.LoadWorkflowContext = activityDefinition.LoadWorkflowContext;
//             activity.SaveWorkflowContext = activityDefinition.SaveWorkflowContext;
//             return activity;
//         }
//
//         public IEnumerable<Type> GetActivityTypes() => ActivityTypeLookup.Values.ToList();
//         public Type? GetActivityType(string activityTypeName) => ActivityTypeLookup.ContainsKey(activityTypeName) ? ActivityTypeLookup[activityTypeName] : default;
//     }
// }
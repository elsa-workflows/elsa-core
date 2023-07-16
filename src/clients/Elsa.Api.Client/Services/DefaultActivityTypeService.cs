// using Elsa.Api.Client.Activities;
// using Elsa.Api.Client.Contracts;
//
// namespace Elsa.Api.Client.Services;
//
// /// <summary>
// /// Provides a default implementation of <see cref="IActivityTypeService"/> that creates a <see cref="Activity"/>.
// /// </summary>
// public class DefaultActivityTypeService : IActivityTypeService
// {
//     private readonly IEnumerable<IActivityTypeResolver> _activityProviders;
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="DefaultActivityTypeService"/> class.
//     /// </summary>
//     /// <param name="activityProviders">The <see cref="IActivityTypeResolver"/>s.</param>
//     public DefaultActivityTypeService(IEnumerable<IActivityTypeResolver> activityProviders)
//     {
//         _activityProviders = activityProviders.OrderByDescending(x => x.Priority);
//     }
//
//     /// <inheritdoc />
//     public Type ResolveType(string activityType)
//     {
//         var context = new ActivityTypeResolverContext(activityType);
//         var activityProvider = GetActivityProvider(context);
//
//         if (activityProvider == null)
//             throw new Exception($"No activity provider found for activity type '{activityType}'.");
//
//         return activityProvider.ResolveType(context);
//     }
//
//     private IActivityTypeResolver? GetActivityProvider(ActivityTypeResolverContext context) => _activityProviders.FirstOrDefault(provider => provider.GetSupportsType(context));
// }
using System;
using Elsa.Dashboard.Options;
using Elsa.Metadata;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Elsa.Dashboard.Extensions
{
    public static class ActivityDefinitionListExtensions
    {
        public static ActivityDefinitionList Add<T>(this ActivityDefinitionList list) where T : IActivity
        {
            return list.Add(ActivityDescriber.Describe<T>());
        }

        public static ActivityDefinitionList Discover(
            this ActivityDefinitionList list,
            Action<ITypeSourceSelector> selector)
        {
            var typeSourceSelector = new TypeSourceSelector();
            selector(typeSourceSelector);

            var serviceCollection = new ServiceCollection();
            typeSourceSelector.Populate(serviceCollection, RegistrationStrategy.Replace(ReplacementBehavior.All));

            foreach (var service in serviceCollection)
            {
                list.Add(ActivityDescriber.Describe(service.ImplementationType));
            }

            return list;
        }

        public static ElsaDashboardOptions DiscoverActivities(
            this ElsaDashboardOptions options)
        {
            options.ActivityDefinitions
                // Add all activities from all referenced assemblies.
                .Discover(
                    selector => selector.FromApplicationDependencies(x => !x.FullName.StartsWith("Microsoft.AspNetCore")) // TODO: Prevents type load exception. Needs more investigation.
                        .AddClasses(x => x.AssignableTo<IActivity>()));

            return options;
        }
    }
}
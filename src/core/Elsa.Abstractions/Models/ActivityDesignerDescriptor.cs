using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Microsoft.Extensions.Localization;

namespace Elsa.Models
{
    public class ActivityDesignerDescriptor
    {
        public static string GetDisplayName<T>() where T : IActivity => GetDisplayName(typeof(T));
        public static string GetDisplayName(Type type) => type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.Name;
        public static string GetDescription<T>() where T : IActivity => GetDescription(typeof(T));
        public static string GetDescription(Type type) => type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "";
        public static string GetCategory<T>() where T : IActivity => GetCategory(typeof(T));
        public static string GetCategory(Type type) => type.GetCustomAttribute<CategoryAttribute>()?.Category ?? "Miscellaneous";
        public static IReadOnlyCollection<string> GetEndpoints<T>() where T : IActivity => GetEndpoints(typeof(T));
        public static IReadOnlyCollection<string> GetEndpoints(Type type) => type.GetCustomAttribute<EndpointsAttribute>()?.Endpoints ?? new[]{ EndpointNames.Done };
        
        public static ActivityDesignerDescriptor For<T>(IStringLocalizer localizer, Func<T, IEnumerable<LocalizedString>> endpointsFactory = null) where T : IActivity =>
            For(typeof(T), localizer, endpointsFactory != null 
                ? (Func<IActivity, IEnumerable<LocalizedString>>)(a => endpointsFactory((T)a)) : 
                (a => GetEndpoints<T>().Select(x => localizer[x])));

        public static ActivityDesignerDescriptor For(Type type, IStringLocalizer localizer, Func<IActivity, IEnumerable<LocalizedString>> endpointsFactory = null)
        {
            var activityTypeName = type.Name;
            var displayName = GetDisplayName(type);
            var category = GetCategory(type);
            var description = GetDescription(type);
            var endpoints = GetEndpoints(type);

            return new ActivityDesignerDescriptor(
                activityTypeName,
                localizer[category],
                localizer[displayName],
                localizer[description],
                endpointsFactory ?? (_ => endpoints.Select(x => localizer[x]))
            );
        }

        public static ActivityDesignerDescriptor For(
            string activityTypeName,
            LocalizedString category,
            LocalizedString displayName,
            LocalizedString description,
            params LocalizedString[] endpoints
        ) => new ActivityDesignerDescriptor(
            activityTypeName,
            displayName,
            category,
            description,
            _ => endpoints
        );

        public ActivityDesignerDescriptor()
        {
        }

        public ActivityDesignerDescriptor(
            string activityTypeName,
            LocalizedString category,
            LocalizedString displayName,
            LocalizedString description,
            Func<IActivity, IEnumerable<LocalizedString>> endPoints)
        {
            ActivityTypeName = activityTypeName;
            Category = category;
            DisplayName = displayName;
            Description = description;
            EndPoints = endPoints;
        }

        public string ActivityTypeName { get; set; }
        public LocalizedString DisplayName { get; set; }
        public LocalizedString Category { get; set; }
        public LocalizedString Description { get; set; }
        public Func<IActivity, IEnumerable<LocalizedString>> EndPoints { get; set; } = _ => Enumerable.Empty<LocalizedString>();
        public bool IsBrowsable { get; set; } = true;
    }
}
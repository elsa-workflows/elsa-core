using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Results;
using Microsoft.Extensions.Localization;

namespace Elsa.Models
{
    public class ActivityDescriptor
    {
        public static ActivityDescriptor For<T>(
            LocalizedString category, 
            LocalizedString displayText, 
            LocalizedString description,
            params LocalizedString[] endpoints)
        {
            return new ActivityDescriptor(typeof(T), category, displayText, description, endpoints);
        }

        public ActivityDescriptor()
        {
        }
        
        public ActivityDescriptor(
            Type activityType, 
            LocalizedString category, 
            LocalizedString displayText, 
            LocalizedString description,
            params LocalizedString[] endpoints)
        {
            ActivityType = activityType;
            Name = activityType.Name;
            Category = category;
            DisplayText = displayText;
            Description = description;
            GetEndpoints = a => endpoints;
        }

        public bool IsBrowsable { get; set; } = true;
        public bool IsTrigger { get; set; }
        public string Name { get; set; }
        public LocalizedString Category { get; set; }
        public Type ActivityType { get; set; }
        public LocalizedString DisplayText { get; set; }
        public LocalizedString Description { get; set; }
        public Func<IActivity, IEnumerable<LocalizedString>> GetEndpoints { get; set; } = a => Enumerable.Empty<LocalizedString>();
    }
}
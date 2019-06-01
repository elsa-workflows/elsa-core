using System;
using System.Collections.Generic;
using System.Reflection;
using Elsa.Attributes;
using Microsoft.Extensions.Localization;

namespace Elsa.Models
{
    public class ActivityDescriptor
    {
        public static ActivityDescriptor For<T>() where T : IActivity => For(typeof(T));
        
        public static ActivityDescriptor For(Type activityModelType)
        {
            var isTriggerAttribute = activityModelType.GetCustomAttribute<IsTriggerAttribute>();
            var isTrigger = isTriggerAttribute?.IsTrigger ?? false;
            
            return new ActivityDescriptor(activityModelType.Name, activityModelType, isTrigger);
        }

        public ActivityDescriptor()
        {
        }

        public ActivityDescriptor(
            string activityTypeName,
            Type activityModelType,
            bool isTrigger)
        {
            ActivityTypeName = activityTypeName;
            ActivityModelType = activityModelType;
            IsTrigger = isTrigger;
        }

        
        public bool IsTrigger { get; }
        public string ActivityTypeName { get; }
        public Type ActivityModelType { get; }
    }
}
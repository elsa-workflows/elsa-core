using Elsa.Models;
using Elsa.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa
{
    public class ElsaOptions
    {
        /// <summary>
        /// The set of activities available to workflows.
        /// </summary>
        private readonly TypeList _activities = new TypeList();

        public IEnumerable<Type> Activities => _activities.ToList().AsReadOnly();

        public ElsaOptions RegisterActivity<T>() where T : IActivity, new()
        {
            return RegisterActivity(typeof(T));

        }

        public ElsaOptions RegisterActivity(Type activityType)
        {
            if(IsActivityRegistered(activityType) == false)
            {
                _activities.Add(activityType);
            }

            return this;
        }

        public ElsaOptions UnregisterActivityType<T>() where T : IActivity, new()
        {
            return UnregisterActivityType(typeof(T));
        }

        public ElsaOptions UnregisterActivityType(Type activityType)
        {
            if (IsActivityRegistered(activityType) == false)
            {
                _activities.RemoveWhere(type => type.FullName == activityType.FullName);
            }
            
            return this;
        }

        public bool IsActivityRegistered(Type activityType)
        {
            return _activities.Contains(activityType);
        }
    }
}

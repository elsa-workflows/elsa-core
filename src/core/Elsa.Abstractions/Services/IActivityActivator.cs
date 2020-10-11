using System;
using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IActivityActivator
    {
        IActivity ActivateActivity(string activityTypeName, Action<IActivity>? setup = default);
        T ActivateActivity<T>(Action<T>? configure = default) where T : class, IActivity;
        IActivity ActivateActivity(ActivityDefinition activityDefinition);
        IEnumerable<Type> GetActivityTypes();
        Type? GetActivityType(string activityTypeName);
    }
}
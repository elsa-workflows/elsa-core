using System;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityResolver
    {
        IActivity ResolveActivity(string activityTypeName, Action<IActivity> setup = null);
        T ResolveActivity<T>(Action<T> configure = null) where T : class, IActivity;
    }
}
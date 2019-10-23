using System;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityResolver
    {
        IActivity ResolveActivity(string activityTypeName, Action<IActivity> setup = default);
        T ResolveActivity<T>(Action<T> configure = default) where T : class, IActivity;
    }
}
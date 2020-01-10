using System;
using Elsa.Services;

namespace Elsa.Builders
{
    public interface IBuilder
    {
        ActivityBuilder Then<T>(Action<T>? setup = default, Action<ActivityBuilder> branch = default) where T : class, IActivity;
        ActivityBuilder Then<T>(T activity, Action<ActivityBuilder> branch = default) where T : class, IActivity;
    }
}
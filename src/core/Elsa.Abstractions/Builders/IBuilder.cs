using System;
using Elsa.Services;

namespace Elsa.Builders
{
    public interface IBuilder
    {
        IActivityBuilder Then<T>(Action<T>? setup = default, Action<IActivityBuilder>? branch = default) where T : class, IActivity;
        IActivityBuilder Then<T>(T activity, Action<IActivityBuilder>? branch = default) where T : class, IActivity;
    }
}
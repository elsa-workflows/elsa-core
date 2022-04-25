using System.Text.Json;
using Elsa.Management.Models;
using Elsa.Management.Services;
using Elsa.Services;

namespace Elsa.Management.Implementations;

public class ActivityFactory : IActivityFactory
{
    public IActivity Create(Type type, ActivityConstructorContext context)
    {
        var activity = (IActivity)context.Element.Deserialize(type, context.SerializerOptions)!;
        return activity;
    }
}
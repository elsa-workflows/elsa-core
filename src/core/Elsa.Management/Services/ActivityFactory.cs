using System.Text.Json;
using Elsa.Contracts;
using Elsa.Management.Contracts;
using Elsa.Management.Models;

namespace Elsa.Management.Services;

public class ActivityFactory : IActivityFactory
{
    public IActivity Create(Type type, ActivityConstructorContext context)
    {
        var activity = (IActivity)context.Element.Deserialize(type, context.SerializerOptions)!;
        return activity;
    }
}
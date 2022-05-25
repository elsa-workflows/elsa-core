using System.Text.Json;
using Elsa.Services;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

public class ActivityFactory : IActivityFactory
{
    public IActivity Create(Type type, ActivityConstructorContext context)
    {
        var activity = (IActivity)context.Element.Deserialize(type, context.SerializerOptions)!;
        return activity;
    }
}
using System.Text.Json;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Contracts;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class ActivityFactory : IActivityFactory
{
    /// <inheritdoc />
    public IActivity Create(Type type, ActivityConstructorContext context)
    {
        var activity = (IActivity)context.Element.Deserialize(type, context.SerializerOptions)!;
        return activity;
    }
}
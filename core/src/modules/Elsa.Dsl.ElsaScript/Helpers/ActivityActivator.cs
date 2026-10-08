using Elsa.Workflows;

namespace Elsa.Dsl.ElsaScript.Helpers;

internal static class ActivityActivator
{
    public static IActivity Create(Type type)
    {
        // Try parameterless ctor first (fast path)
        var parameterless = type.GetConstructor(Type.EmptyTypes);
        if (parameterless != null)
            return (IActivity)parameterless.Invoke([]);

        // Fall back: pick a public ctor and provide default values for args
        var ctor = type
                       .GetConstructors()
                       .OrderBy(c => c.GetParameters().Length) // shortest first
                       .FirstOrDefault()
                   ?? throw new InvalidOperationException(
                       $"Type {type} has no public constructors and cannot be instantiated.");

        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];

            if (p.HasDefaultValue)
            {
                args[i] = p.DefaultValue;
            }
            else
            {
                args[i] = p.ParameterType.IsValueType
                    ? Activator.CreateInstance(p.ParameterType)
                    : null;
            }
        }

        return (IActivity)ctor.Invoke(args);
    }
}
using Elsa.Expressions.PowerFx.Features;
using Elsa.Features.Services;

namespace Elsa.Expressions.PowerFx.Extensions;

/// <summary>
/// Extension methods for <see cref="IModule"/>.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds Power Fx expressions support.
    /// </summary>
    public static IModule UsePowerFx(this IModule module)
    {
        module.Use<PowerFxFeature>();
        return module;
    }
}
using Elsa.Alterations.Features;
using Elsa.Alterations.MassTransit.Features;
using Elsa.Extensions;
using Elsa.Features.Services;

namespace Elsa.Alterations.MassTransit.Extensions;

/// <summary>
/// Adds the <see cref="AlterationsFeature"/>.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the <see cref="MassTransitAlterationsFeature"/>.
    /// </summary>
    public static IModule UseMassTransitDispatcher(this AlterationsFeature alterations, Action<MassTransitAlterationsFeature>? configure = default) => alterations.Module.Use(configure);
}
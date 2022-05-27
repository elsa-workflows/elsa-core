using System;
using Elsa.Features.Services;
using Elsa.Http.Features;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IModule UseHttp(this IModule module, Action<HttpFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}
using Elsa.Dsl.ElsaScript.Compiler;
using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Dsl.ElsaScript.Parser;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.ElsaScript.Features;

/// <summary>
/// Feature for ElsaScript DSL support.
/// </summary>
public class ElsaScriptFeature : FeatureBase
{
    /// <inheritdoc />
    public ElsaScriptFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        // No additional configuration needed
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<IElsaScriptParser, ElsaScriptParser>();
        Services.AddSingleton<IElsaScriptCompiler, ElsaScriptCompiler>();
    }
}

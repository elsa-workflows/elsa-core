using CShells.Features;
using Elsa.Dsl.ElsaScript.Compiler;
using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Dsl.ElsaScript.Materializers;
using Elsa.Dsl.ElsaScript.Parser;
using Elsa.Workflows.Management;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.ElsaScript.ShellFeatures;

/// <summary>
/// Feature for ElsaScript DSL support.
/// </summary>
[ShellFeature(
    DisplayName = "ElsaScript DSL",
    Description = "Provides ElsaScript DSL support for defining workflows")]
[UsedImplicitly]
public class ElsaScriptFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IElsaScriptParser, ElsaScriptParser>();
        services.AddScoped<IElsaScriptCompiler, ElsaScriptCompiler>();
        services.AddScoped<IWorkflowMaterializer, ElsaScriptWorkflowMaterializer>();
    }
}


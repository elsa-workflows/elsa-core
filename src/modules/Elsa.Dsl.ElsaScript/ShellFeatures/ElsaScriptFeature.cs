using Elsa.Dsl.ElsaScript.Compiler;
using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Dsl.ElsaScript.Materializers;
using Elsa.Dsl.ElsaScript.Parser;
using Elsa.Workflows.Management;
using CShells.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.ElsaScript.ShellFeatures;

/// <summary>
/// Feature for ElsaScript DSL support.
/// </summary>
[ShellFeature(
    DisplayName = "ElsaScript DSL",
    Description = "Provides DSL support for defining workflows in ElsaScript format")]
public class ElsaScriptFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IElsaScriptParser, ElsaScriptParser>();
        services.AddScoped<IElsaScriptCompiler, ElsaScriptCompiler>();
        services.AddScoped<IWorkflowMaterializer, ElsaScriptWorkflowMaterializer>();
    }
}

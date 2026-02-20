using System.Reflection;
using CShells.Features;
using Elsa.Expressions.JavaScript.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.JavaScript.Libraries.ShellFeatures;

/// <summary>
/// Base class for JavaScript library shell features.
/// </summary>
public abstract class ScriptModuleShellFeatureBase : IShellFeature
{
    private readonly string _moduleName;

    protected ScriptModuleShellFeatureBase(string moduleName)
    {
        _moduleName = moduleName;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var moduleName = _moduleName;
        services.Configure<JintOptions>(options =>
        {
            options.ConfigureEngine((engine, context) =>
            {
                var resourceName = $"Elsa.Expressions.JavaScript.Libraries.ClientLib.dist.{moduleName}.js";
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
                using var reader = new StreamReader(stream);
                var script = reader.ReadToEnd();
                engine.Execute(script);
            });
        });
    }
}

/// <summary>
/// Adds Lodash library support to JavaScript expressions.
/// </summary>
[ShellFeature(
    DisplayName = "Lodash JavaScript Library",
    Description = "Provides Lodash utility library for JavaScript expressions",
    DependsOn = ["JavaScript"])]
[UsedImplicitly]
public class LodashFeature : ScriptModuleShellFeatureBase
{
    public LodashFeature() : base("lodash") { }
}

/// <summary>
/// Adds Lodash FP library support to JavaScript expressions.
/// </summary>
[ShellFeature(
    DisplayName = "Lodash FP JavaScript Library",
    Description = "Provides Lodash FP (functional programming) utility library for JavaScript expressions",
    DependsOn = ["JavaScript"])]
[UsedImplicitly]
public class LodashFpFeature : ScriptModuleShellFeatureBase
{
    public LodashFpFeature() : base("lodashFp") { }
}

/// <summary>
/// Adds Moment.js library support to JavaScript expressions.
/// </summary>
[ShellFeature(
    DisplayName = "Moment JavaScript Library",
    Description = "Provides Moment.js date/time library for JavaScript expressions",
    DependsOn = ["JavaScript"])]
[UsedImplicitly]
public class MomentFeature : ScriptModuleShellFeatureBase
{
    public MomentFeature() : base("moment") { }
}


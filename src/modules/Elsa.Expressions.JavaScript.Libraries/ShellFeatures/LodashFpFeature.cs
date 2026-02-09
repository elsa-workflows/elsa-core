using System.Reflection;
using CShells.Features;
using Elsa.Expressions.JavaScript.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.JavaScript.Libraries.ShellFeatures;

/// <summary>
/// Adds Lodash FP library support to JavaScript expressions.
/// </summary>
[ShellFeature(
    DisplayName = "Lodash FP Library",
    Description = "Adds Lodash Functional Programming library to JavaScript expressions",
    DependsOn = ["JavaScript"])]
public class LodashFpFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<JintOptions>(options =>
        {
            options.ConfigureEngine((engine, context) =>
            {
                var resourceName = "Elsa.Expressions.JavaScript.Libraries.ClientLib.dist.lodashFp.js";
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
                using var reader = new StreamReader(stream);
                var script = reader.ReadToEnd();
                engine.Execute(script);
            });
        });
    }
}

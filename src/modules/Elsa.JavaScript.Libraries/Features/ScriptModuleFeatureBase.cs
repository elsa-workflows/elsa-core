using System.Reflection;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Features;

namespace Elsa.JavaScript.Libraries;

[DependsOn(typeof(JavaScriptFeature))]
public abstract class ScriptModuleFeatureBase(string moduleName, IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.UseJavaScript(jintOptions =>
        {
            jintOptions.ConfigureEngine((engine, context) =>
            {
                var resourceName = $"Elsa.JavaScript.Libraries.ClientLib.dist.{moduleName}.js";
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
                using var reader = new StreamReader(stream);
                var script = reader.ReadToEnd();
                engine.Execute(script);
            });
        });
    }
}
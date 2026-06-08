using System.ComponentModel;
using CShells.Features;
using Elsa.Common.Serialization;
using Elsa.Common.Services;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

[ManifestFeatureCategory("Infrastructure")]
[ShellFeature(
    "DefaultFormatters",
    DisplayName = "Default Formatters",
    Description = "Registers default serializers and type converters")]
public class DefaultFormattersFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        TypeDescriptor.AddAttributes(typeof(Type), new TypeConverterAttribute(typeof(TypeTypeConverter)));
        services.AddSingleton<IFormatter, JsonFormatter>();
    }
}

using System.ComponentModel;
using CShells.Features;
using Elsa.Common.Serialization;
using Elsa.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

[ShellFeature("DefaultFormatters")]
public class DefaultFormattersFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        TypeDescriptor.AddAttributes(typeof(Type), new TypeConverterAttribute(typeof(TypeTypeConverter)));
        services.AddSingleton<IFormatter, JsonFormatter>();
    }
}
using System.Collections;
using System.ComponentModel;
using Elsa.Common.Serialization;
using Elsa.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

public class DefaultFormattersFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        TypeDescriptor.AddAttributes(typeof(IEnumerable), new TypeConverterAttribute(typeof(IEnumerableTypeConverter)));
        TypeDescriptor.AddAttributes(typeof(Type), new TypeConverterAttribute(typeof(TypeTypeConverter)));
        Module.Services.AddSingleton<IFormatter, JsonFormatter>();
    }
}
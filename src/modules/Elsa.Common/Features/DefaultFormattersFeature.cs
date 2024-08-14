using Elsa.Common.Contracts;
using Elsa.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

public class DefaultFormattersFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        module.Services.AddSingleton<IFormatter, JsonFormatter>();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Command.Features;
[DependsOn(typeof(MediatorFeature))]
public class CommandExecuterFeature : FeatureBase
{
    /// <inheritdoc />
    public CommandExecuterFeature(IModule module) : base(module)
    {

    }

    /// <inheritdoc />
    public override void Apply()
    {
        //// C# services.
        //Services
        //    .AddExpressionDescriptorProvider<CSharpExpressionDescriptorProvider>()
        //    .AddScoped<ICSharpEvaluator, CSharpEvaluator>()
        //    ;

        // Handlers.
        Services.AddNotificationHandlersFrom<CommandExecuterFeature>();

        // Activities.
        Module.AddActivitiesFrom<CommandExecuterFeature>();

        // UI property handlers.
     //   Services.AddScoped<IPropertyUIHandler, RunCSharpOptionsProvider>();
    }
}
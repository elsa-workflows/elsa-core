using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.CommandExecuter.Providers;
using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows;
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

        Services.AddNotificationHandlersFrom<CommandExecuterFeature>();
        Module.AddActivitiesFrom<CommandExecuterFeature>();
        Services.AddScoped<IPropertyUIHandler, WorkflowCommandProvider>();
    }
}
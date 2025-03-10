using Elsa.CLI.Activities;
using Elsa.CLI.Contracts;
using Elsa.CLI.Options;
using Elsa.CLI.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.CLI.Features;

/// <summary>
/// Setup CLI features.
/// </summary>
public class CliFeature : FeatureBase
{
    /// <inheritdoc />
    public CliFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Set a callback to configure <see cref="CliOptions"/>.
    /// </summary>
    public Action<CliOptions> ConfigureOptions { get; set; } = _ => { };

    /// <summary>
    /// Gets or sets a callback for configuring the <see cref="ICommandValidator"/> implementation.
    /// </summary>
    public Func<IServiceProvider, ICommandValidator> CommandValidator { get; set; } = sp => sp.GetRequiredService<DefaultCommandValidator>();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivity<InvokeCommand>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .Configure(ConfigureOptions)
            .AddSingleton<DefaultCommandValidator>()
            .AddSingleton(CommandValidator)
            .AddTransient<CommandFinder>()
            .AddTransient<ICommandRunner, CliWrapCommandRunner>();
    }
}
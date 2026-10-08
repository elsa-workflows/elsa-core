using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Ldap.Contracts;
using Elsa.Ldap.Options;
using Elsa.Ldap.Services;
using Elsa.Ldap.UIHints;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Ldap.Features;

/// <summary>
/// A feature that provides LDAP activities for integrations to LDAP servers (e.g. Active Directory).
/// </summary>
public class LdapFeature : FeatureBase
{
    /// <inheritdoc/>
    public LdapFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Set a callback to configure <see cref="LdapOptions"/>.
    /// </summary>
    public Action<LdapOptions> ConfigureOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<LdapFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .Configure(ConfigureOptions)
            .AddScoped<ILdapConnectionFactory, LdapConnectionFactory>()
            .AddScoped<IPropertyUIHandler, LdapConnectionDropdownOptionsProvider>();
    }
}
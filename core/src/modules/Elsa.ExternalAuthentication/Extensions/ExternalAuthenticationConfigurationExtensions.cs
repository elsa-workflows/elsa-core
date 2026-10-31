using System.Text.Json;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Microsoft.Extensions.Configuration;

namespace Elsa.Extensions;

public static class ExternalAuthenticationConfigurationExtensions
{
    /// <summary>
    /// Binds external-authentication options and reconstructs configuration-backed JSON settings.
    /// </summary>
    public static void BindExternalAuthenticationOptions(this IConfigurationSection section, ExternalAuthenticationOptions options)
    {
        section.Bind(options);

        var connectionSections = GetIndexedChildren(section.GetSection("Connections"));
        var connections = options.ConfigurationConnections.ToArray();
        for (var i = 0; i < Math.Min(connectionSections.Length, connections.Length); i++)
            BindJsonSettings(connectionSections[i], connections[i]);
    }

    private static void BindJsonSettings(IConfigurationSection section, IdentityProviderConnection connection)
    {
        connection.AdapterSettings = GetJsonElement(section, "AdapterSettings");

        if (connection.UnlinkedPolicy is not null)
            connection.UnlinkedPolicy = connection.UnlinkedPolicy with { Settings = GetJsonElement(section.GetSection("UnlinkedPolicy"), "Settings") };

        var grantSourceSections = GetIndexedChildren(section.GetSection("PermissionGrantSources"));
        var grantSources = connection.PermissionGrantSources.ToArray();
        connection.PermissionGrantSources = grantSources
            .Select((source, index) => index < grantSourceSections.Length
                ? source with { Settings = GetJsonElement(grantSourceSections[index], "Settings") }
                : source)
            .ToArray();
    }

    private static JsonElement GetJsonElement(IConfiguration configuration, string sectionKey)
    {
        var json = configuration.GetSectionAsJson(sectionKey);
        return json is null ? default : JsonSerializer.Deserialize<JsonElement>(json);
    }

    private static IConfigurationSection[] GetIndexedChildren(IConfigurationSection section) =>
        section.GetChildren()
            .OrderBy(child => int.TryParse(child.Key, out var index) ? index : int.MaxValue)
            .ToArray();
}

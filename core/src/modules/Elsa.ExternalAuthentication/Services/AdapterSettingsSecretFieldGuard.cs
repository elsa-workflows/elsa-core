using System.Text.Json;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Prevents adapter setting fields that are declared as secret bindings from being persisted or returned as ordinary settings.
/// </summary>
public static class AdapterSettingsSecretFieldGuard
{
    /// <summary>Throws when a descriptor-declared secret is supplied through an adapter settings document.</summary>
    public static void ThrowIfContainsDeclaredSecret(JsonElement settings, ExternalAuthenticationAdapterDescriptor descriptor, string connectionKey)
    {
        if (settings.ValueKind != JsonValueKind.Object)
            return;

        var names = GetSecretFieldNames(descriptor);
        var name = names.FirstOrDefault(name => settings.TryGetProperty(name, out _));
        if (name is not null)
            throw new InvalidOperationException($"Configuration connection '{connectionKey}' supplies secret field '{name}' through AdapterSettings. Configure it through SecretBindings instead.");
    }

    /// <summary>Returns a settings document with descriptor-declared secret fields redacted.</summary>
    public static JsonElement RedactDeclaredSecrets(JsonElement settings, ExternalAuthenticationAdapterDescriptor descriptor)
    {
        if (settings.ValueKind != JsonValueKind.Object)
            return settings.ValueKind == JsonValueKind.Undefined ? default : settings.Clone();

        var names = GetSecretFieldNames(descriptor);
        if (names.Count == 0 || !names.Any(name => settings.TryGetProperty(name, out _)))
            return settings.Clone();

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in settings.EnumerateObject())
            {
                writer.WritePropertyName(property.Name);
                if (names.Contains(property.Name))
                    writer.WriteStringValue(ExternalAuthenticationRedactor.RedactedValue);
                else
                    property.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    private static HashSet<string> GetSecretFieldNames(ExternalAuthenticationAdapterDescriptor descriptor) =>
        descriptor.Fields
            .Where(x => x.IsSecretBinding)
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);
}

using System.Reflection;
using Confluent.SchemaRegistry;
using Elsa.Workflows.UIHints.Dropdown;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.ServiceBus.Kafka.UIHints;

/// <summary>
/// Populates the SchemaFullName dropdown with Avro record full names fetched from all configured
/// schema registries. Always includes a leading "(any)" option so the user can leave filtering disabled.
/// Fails gracefully when no schema registries are configured or a registry is unreachable.
/// </summary>
public class SchemaFullNameDropdownOptionsProvider(ISchemaRegistryDefinitionEnumerator registryEnumerator, IOptions<KafkaOptions> options, ILogger<SchemaFullNameDropdownOptionsProvider> logger) : DropDownOptionsProviderBase
{
    private static readonly SelectListItem AnyOption = new("(any)", "");

    protected override async ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var items = new List<SelectListItem> { AnyOption };
        var registries = await registryEnumerator.EnumerateAsync(cancellationToken);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var prefix = options.Value.SchemaFullNamePrefix;

        foreach (var registry in registries)
        {
            // SaslInherit bridges credentials from a ConsumerConfig we don't have here; skip silently.
            if (registry.Config.BasicAuthCredentialsSource == AuthCredentialsSource.SaslInherit)
                continue;

            try
            {
                using var client = new CachedSchemaRegistryClient(registry.Config);
                var subjects = await client.GetAllSubjectsAsync();

                foreach (var subject in subjects)
                {
                    // Key subjects are typically primitives (string/long), not interesting for filtering.
                    if (subject.EndsWith("-key", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        var registered = await client.GetLatestSchemaAsync(subject);

                        if (registered.SchemaType != SchemaType.Avro)
                            continue;

                        var schema = Avro.Schema.Parse(registered.SchemaString);

                        if (schema is not Avro.RecordSchema recordSchema)
                            continue;

                        var fullName = recordSchema.Fullname;

                        if (string.IsNullOrWhiteSpace(fullName))
                            continue;

                        if (!string.IsNullOrEmpty(prefix) && !fullName.StartsWith(prefix, StringComparison.Ordinal))
                            continue;

                        if (seen.Add(fullName))
                            items.Add(new SelectListItem(fullName, fullName));
                    }
                    catch (Exception ex)
                    {
                        // Subject schema unreachable or unparseable; skip it.
                        logger.LogWarning(ex, "Failed to fetch or parse schema for subject '{Subject}' in registry '{RegistryId}'.", subject, registry.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                // Registry unreachable or misconfigured; skip it and keep the "(any)" option.
                logger.LogWarning(ex, "Failed to enumerate schemas from registry '{RegistryId}'. The dropdown will not include schemas from this registry.", registry.Id);
            }
        }

        return items;
    }
}

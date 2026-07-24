using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

public sealed class DefaultUnlinkedIdentityPolicyRegistry : IUnlinkedIdentityPolicyRegistry
{
    private readonly IReadOnlyDictionary<string, IUnlinkedIdentityPolicy> _policies;
    private readonly IReadOnlyCollection<UnlinkedIdentityPolicyDescriptor> _descriptors;

    public DefaultUnlinkedIdentityPolicyRegistry(
        IEnumerable<IUnlinkedIdentityPolicy> policies,
        ExtensionDescriptorValidator descriptorValidator,
        IOptions<ExternalAuthenticationOptions> options)
    {
        _policies = ExtensionRegistryBuilder.Create(
            policies,
            policy => policy.Type,
            options.Value.AllowedUnlinkedIdentityPolicyTypes,
            "unlinked identity policy");
        _descriptors = _policies.Values
            .Select(descriptorValidator.Validate)
            .OrderBy(x => x.Type, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyCollection<UnlinkedIdentityPolicyDescriptor> ListDescriptors() => _descriptors;
    public bool TryGet(string type, out IUnlinkedIdentityPolicy policy) => _policies.TryGetValue(type, out policy!);
}

public sealed class DefaultPermissionGrantSourceRegistry : IPermissionGrantSourceRegistry
{
    private readonly IReadOnlyDictionary<string, IPermissionGrantSource> _sources;
    private readonly IReadOnlyCollection<PermissionGrantSourceDescriptor> _descriptors;

    public DefaultPermissionGrantSourceRegistry(
        IEnumerable<IPermissionGrantSource> sources,
        ExtensionDescriptorValidator descriptorValidator,
        IOptions<ExternalAuthenticationOptions> options)
    {
        _sources = ExtensionRegistryBuilder.Create(
            sources,
            source => source.Type,
            options.Value.AllowedPermissionGrantSourceTypes,
            "permission grant source");
        _descriptors = _sources.Values
            .Select(descriptorValidator.Validate)
            .OrderBy(x => x.Type, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyCollection<PermissionGrantSourceDescriptor> ListDescriptors() => _descriptors;
    public bool TryGet(string type, out IPermissionGrantSource source) => _sources.TryGetValue(type, out source!);
}

internal static class ExtensionRegistryBuilder
{
    public static IReadOnlyDictionary<string, TExtension> Create<TExtension>(
        IEnumerable<TExtension> extensions,
        Func<TExtension, string> getType,
        IEnumerable<string> allowedTypes,
        string kind)
    {
        var installed = new Dictionary<string, TExtension>(StringComparer.Ordinal);
        foreach (var extension in extensions)
        {
            var type = getType(extension);
            if (string.IsNullOrWhiteSpace(type))
                throw new InvalidOperationException($"An installed {kind} has an empty type.");
            if (!installed.TryAdd(type, extension))
                throw new InvalidOperationException($"The installed {kind} type '{type}' is registered more than once.");
        }

        var allowed = allowedTypes.ToHashSet(StringComparer.Ordinal);
        if (allowed.Count == 0)
            return installed;

        foreach (var type in allowed)
        {
            if (!installed.ContainsKey(type))
                throw new InvalidOperationException($"The allowed {kind} type '{type}' is not installed.");
        }

        return installed
            .Where(x => allowed.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
    }
}

using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Permissions;

public sealed class GroupMappingPermissionGrantSource : IPermissionGrantSource
{
    public const string SourceType = "group-mapping";
    public string Type => SourceType;

    public PermissionGrantSourceDescriptor Describe() => new(Type, "Group mapping", "Grants explicitly mapped Elsa permissions for projected external group claims.", 1, BuiltInPermissionGrantSourceDescriptors.Mapping("groups"), null);

    public ValueTask<PermissionGrantResult> GetGrantsAsync(PermissionGrantContext context, CancellationToken cancellationToken = default) => ValueTask.FromResult(ClaimMappingPermissionGrantSource.CreateResult(context, Type));
}

using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Permissions;

public sealed class ExternalAuthenticationPermissionDescriptorProvider : IPermissionDescriptorProvider
{
    public IEnumerable<PermissionDescriptor> GetDescriptors() => ExternalAuthenticationPermissions.Descriptors;
}

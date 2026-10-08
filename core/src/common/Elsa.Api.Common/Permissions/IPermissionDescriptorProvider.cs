namespace Elsa.Permissions;

/// <summary>
/// Contributes the permission descriptors for one module. Every module exposing protected endpoints
/// implements this, declaring its resources alongside the constants its endpoints reference so the two
/// cannot drift.
/// </summary>
public interface IPermissionDescriptorProvider
{
    /// <summary>The descriptors this module contributes.</summary>
    IEnumerable<PermissionDescriptor> GetDescriptors();
}

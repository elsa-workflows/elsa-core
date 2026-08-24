namespace Elsa.Permissions;

/// <summary>The composed catalog of every resource contributed by the installed modules.</summary>
public interface IPermissionDescriptorRegistry
{
    /// <summary>Every registered descriptor, ordered by resource.</summary>
    IReadOnlyCollection<PermissionDescriptor> List();

    /// <summary>The descriptor for <paramref name="resource"/>, or <c>null</c> when none is registered.</summary>
    PermissionDescriptor? Find(string resource);

    /// <summary>
    /// The resources a grant currently covers. A wildcard grant also covers resources registered later;
    /// this reports what is registered now, which is how a role author sees a wildcard's reach.
    /// </summary>
    IReadOnlyCollection<string> Reach(string resourcePattern);
}

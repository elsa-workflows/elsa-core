namespace Elsa.ModularPersistence.Descriptors;

/// <summary>
/// Explicit schema version identity for a storage manifest.
/// </summary>
public sealed record StorageManifestVersion
{
    public StorageManifestVersion(int major, int minor = 0, int patch = 0)
    {
        if (major < 1)
            throw new ArgumentOutOfRangeException(nameof(major), "Major version must be greater than zero.");

        if (minor < 0)
            throw new ArgumentOutOfRangeException(nameof(minor), "Minor version cannot be negative.");

        if (patch < 0)
            throw new ArgumentOutOfRangeException(nameof(patch), "Patch version cannot be negative.");

        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}

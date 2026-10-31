using System.Reflection;

namespace Elsa.ReactStudio.Host;

/// <summary>An enabled server feature's independently built React Studio assets and compatibility contract.</summary>
public sealed record ReactStudioModule(string Id, string Name, string PackageId, bool Core,
    Assembly Assembly, string[] RequiresServerFeatures, int HostApiVersion = 1, int SdkApiVersion = 1)
{
    /// <summary>The immutable asset version carried by this module assembly.</summary>
    public string Version => Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
}

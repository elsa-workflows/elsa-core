using System.Reflection;

namespace Elsa.ReactStudio.Spike;

/// <summary>Experimental module manifest for the #8839 architecture proof, not a supported SDK.</summary>
public sealed record StudioModule(string Id, string Version, int HostMajor, int SdkMajor, Assembly Assembly);

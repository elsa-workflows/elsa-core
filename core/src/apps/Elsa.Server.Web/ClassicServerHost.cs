using JetBrains.Annotations;

namespace Elsa.Server.Web;

/// <summary>
/// Entry-point marker so a test can boot this host with <c>WebApplicationFactory</c>.
/// </summary>
/// <remarks>
/// This assembly already declares a <c>public partial class Program</c> in the global namespace, and so
/// does Elsa.ModularServer.Web, so a test project referencing both hosts cannot name either unambiguously.
/// Each host therefore carries a namespaced marker for that purpose. This does not replace <c>Program</c>,
/// which existing component tests still use.
/// </remarks>
[UsedImplicitly]
public sealed class ClassicServerHost;

using JetBrains.Annotations;

namespace Elsa.ModularServer.Web;

/// <summary>
/// Entry-point marker so a test can boot this host with <c>WebApplicationFactory</c>.
/// </summary>
/// <remarks>
/// The host is built from top-level statements, whose generated entry point is internal, so a test project
/// needs some public type from this assembly to name as the factory's entry point. A dedicated marker is
/// used rather than an incidental public type so that deleting an unrelated class cannot quietly break the
/// smoke test, and rather than a <c>public partial class Program</c> because Elsa.Server.Web already
/// declares one in the global namespace and a test referencing both hosts could not tell them apart.
/// </remarks>
[UsedImplicitly]
public sealed class ModularServerHost;

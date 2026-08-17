using System.Runtime.CompilerServices;

namespace Elsa.Dsl.ElsaScript.IntegrationTests;

/// <summary>
/// Turns Jint's host-contract verifiers on for this test assembly.
/// </summary>
/// <remarks>
/// <para>
/// The verifiers catch a host answering one of Jint's extension points in a way that contradicts another —
/// contradictions the engine cannot afford to re-check on its hot paths and therefore trusts, so a violation is
/// otherwise silent. The one Elsa is exposed to today is the object-converter type declaration: registering a
/// converter with <c>AddObjectConverter(converter, handledTypes)</c> promises the engine that the converter
/// produces values only for those types, and in exchange the compiled interop lanes are kept for every member
/// that cannot produce one. A case added to a converter's own <c>TryConvert</c> switch and not added to its
/// registration is then silently skipped on exactly those members, which nothing else would report.
/// </para>
/// <para>
/// The shipped Jint package is a Release build, where the checks are compiled behind a runtime flag read once at
/// type initialization: with the switch off the JIT folds the guards away entirely, so this costs a production
/// host nothing and there is no Debug build to obtain. It has to be set before the first use of any Jint type,
/// which is what the module initializer is for. Duplicated per scripting test assembly on purpose — putting it
/// in the shared <c>Elsa.Testing.Shared.Integration</c> package would flip a process-wide switch for every
/// external consumer of that package as well.
/// </para>
/// </remarks>
internal static class JintHostContractVerification
{
    [ModuleInitializer]
    internal static void Enable() => AppContext.SetSwitch("Jint.EnableHostContractVerification", true);
}

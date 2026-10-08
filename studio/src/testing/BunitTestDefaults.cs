using System.Runtime.CompilerServices;
using Bunit;

namespace Elsa.Studio.Testing;

/// <summary>
/// Applies repo-wide bUnit defaults to every test assembly that links this file.
/// </summary>
internal static class BunitTestDefaults
{
    /// <summary>
    /// bUnit waits one second by default, which is short enough that a loaded CI runner can trip
    /// <c>WaitForAssertion</c>/<c>WaitForElement</c> before the component has finished rendering.
    /// A passing wait returns as soon as its condition holds, so a longer ceiling only costs time
    /// on waits that were going to fail anyway.
    /// </summary>
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    [ModuleInitializer]
    internal static void Initialize() => BunitContext.DefaultWaitTimeout = WaitTimeout;
}

using Elsa.Studio.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm;

/// <summary>
/// Represents the OpenID Connect feature specific to Blazor WebAssembly authentication
/// within the Elsa Studio platform.
/// </summary>
/// <remarks>
/// This feature integrates OpenID Connect authentication capabilities into the
/// Blazor WebAssembly context, allowing for secure user authentication in a
/// distributed environment. It derives from the <see cref="FeatureBase"/> class,
/// which provides a framework for modules extending the Elsa Studio dashboard.
/// </remarks>
[UsedImplicitly]
public class OpenIdConnectBlazorWasmFeature : FeatureBase;

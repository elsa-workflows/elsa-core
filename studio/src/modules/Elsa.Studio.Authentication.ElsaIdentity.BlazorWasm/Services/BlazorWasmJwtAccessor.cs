using Blazored.LocalStorage;
using Elsa.Studio.Authentication.ElsaIdentity.Services;

namespace Elsa.Studio.Authentication.ElsaIdentity.BlazorWasm.Services;

/// <summary>
/// Blazor WebAssembly implementation of JWT accessor.
/// </summary>
public class BlazorWasmJwtAccessor(ILocalStorageService localStorageService) : JwtAccessorBase(localStorageService);

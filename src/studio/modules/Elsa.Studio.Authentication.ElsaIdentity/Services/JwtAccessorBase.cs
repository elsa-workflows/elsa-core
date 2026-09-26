using Blazored.LocalStorage;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <summary>
/// Abstract base class for JWT token accessor implementations that use local storage.
/// Provides common functionality for reading, writing, and clearing tokens.
/// </summary>
public abstract class JwtAccessorBase(ILocalStorageService localStorageService) : IJwtAccessor
{
    /// <summary>
    /// Determines whether storage operations can be performed.
    /// Override this method to implement platform-specific checks (e.g., prerendering detection).
    /// </summary>
    /// <returns>True if storage can be accessed; otherwise, false.</returns>
    protected virtual bool CanAccessStorage() => true;

    /// <inheritdoc />
    public virtual async ValueTask<string?> ReadTokenAsync(string name)
    {
        if (!CanAccessStorage())
            return null;

        return await localStorageService.GetItemAsync<string>(name);
    }

    /// <inheritdoc />
    public virtual async ValueTask WriteTokenAsync(string name, string token)
    {
        if (!CanAccessStorage())
            return;

        await localStorageService.SetItemAsStringAsync(name, token);
    }

    /// <inheritdoc />
    public virtual async ValueTask ClearTokenAsync(string name)
    {
        if (!CanAccessStorage())
            return;

        await localStorageService.RemoveItemAsync(name);
    }
}

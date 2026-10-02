using Blazored.LocalStorage;
using Elsa.Studio.Login.Contracts;

namespace Elsa.Studio.Login.BlazorWasm.Services;

/// <inheritdoc />
public class BlazorWasmJwtAccessor : IJwtAccessor
{
    private readonly ILocalStorageService _localStorageService;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="localStorageService"></param>
    public BlazorWasmJwtAccessor(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    /// <inheritdoc />
    public async ValueTask<string?> ReadTokenAsync(string name)
    {   
        return await _localStorageService.GetItemAsync<string>(name);
    }

    /// <inheritdoc />
    public async ValueTask WriteTokenAsync(string name, string token)
    {
        await _localStorageService.SetItemAsStringAsync(name, token);
    }
}
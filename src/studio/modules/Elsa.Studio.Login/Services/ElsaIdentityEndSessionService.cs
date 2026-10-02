using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.Services;

///<inheritdoc/>
public class ElsaIdentityEndSessionService(IJwtAccessor jwtAccessor, NavigationManager navigationManager) : IEndSessionService
{
    ///<inheritdoc/>
    public async Task LogoutAsync()
    {
        await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, "");
        await jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, "");
        navigationManager.NavigateTo("/", true);
    }
}

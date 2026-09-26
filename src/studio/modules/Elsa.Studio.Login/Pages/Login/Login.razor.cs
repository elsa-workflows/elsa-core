using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Radzen;

namespace Elsa.Studio.Login.Pages.Login;

/// <summary>
/// The login page.
/// </summary>
[AllowAnonymous]
public partial class Login
{
    [Inject] private IJwtAccessor JwtAccessor { get; set; } = null!;
    [Inject] private ICredentialsValidator CredentialsValidator { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private IClientInformationProvider ClientInformationProvider { get; set; } = null!;
    [Inject] private IServerInformationProvider ServerInformationProvider { get; set; } = null!;
    [Inject] private IUserMessageService UserMessageService { get; set; } = null!;

    private string ClientVersion { get; set; } = "3.0.0";
    private string ServerVersion { get; set; } = "3.0.0";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var clientInformation = await ClientInformationProvider.GetInfoAsync();
        var serverInformation = await ServerInformationProvider.GetInfoAsync();
        ClientVersion = clientInformation.PackageVersion;
        ServerVersion = string.Join('.', serverInformation.PackageVersion.Split('.').Take(2));
    }

    private async Task TryLogin(LoginArgs args)
    {
        var isValid = await ValidateCredentials(args.Username, args.Password);
        if (!isValid)
        {
            UserMessageService.ShowSnackbarTextMessage("Invalid credentials. Please try again", Severity.Error);
            return;
        }

        ((AccessTokenAuthenticationStateProvider)AuthenticationStateProvider).NotifyAuthenticationStateChanged();

        var uri = new Uri(NavigationManager.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("returnUrl", out var returnUrl))
        {
            NavigationManager.NavigateTo(returnUrl.FirstOrDefault() ?? "", true);
        }
        else
        {
            NavigationManager.NavigateTo("", true);
        }
    }

    private async Task<bool> ValidateCredentials(string? username, string? password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return false;
        
        var result = await CredentialsValidator.ValidateCredentialsAsync(username, password);

        if (!result.IsAuthenticated)
            return false;

        await JwtAccessor.WriteTokenAsync(TokenNames.AccessToken, result.AccessToken!);
        await JwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, result.RefreshToken!);
        return true;
    }
}

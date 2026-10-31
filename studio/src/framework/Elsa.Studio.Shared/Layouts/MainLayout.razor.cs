using Elsa.Studio.Branding;
using Elsa.Studio.Components.AppBar;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Elsa.Studio.Layouts;

/// <summary>
/// The main layout for the application.
/// </summary>
public partial class MainLayout : IDisposable
{
    private bool _drawerOpen = true;
    private ErrorBoundary? _errorBoundary;

    [Inject] private IThemeService ThemeService { get; set; } = null!;
    [Inject] private IStudioThemeRegistry ThemeRegistry { get; set; } = null!;
    [Inject] private IAppBarService AppBarService { get; set; } = null!;
    [Inject] private IUnauthorizedComponentProvider UnauthorizedComponentProvider { get; set; } = null!;
    [Inject] private IErrorComponentProvider ErrorComponentProvider { get; set; } = null!;
    [Inject] private IFeatureService FeatureService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IBrandingProvider BrandingProvider { get; set; } = null!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = null!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }
    private MudTheme CurrentTheme => ThemeService.CurrentTheme;
    private bool IsDarkMode => ThemeService.IsDarkMode;
    private string ThemeId => ThemeRegistry.Selected.Id;
    private RenderFragment UnauthorizedComponent => UnauthorizedComponentProvider.GetUnauthorizedComponent();
    private RenderFragment DisplayError(Exception context) => ErrorComponentProvider.GetErrorComponent(context);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (BrandingProvider.AppBarIcons.ShowDocumentationLink) AppBarService.AddComponent<Documentation>(10);
        if (BrandingProvider.AppBarIcons.ShowGitHubLink) AppBarService.AddComponent<GitHub>(15);
        AppBarService.AddComponent<DarkModeToggle>(20);
        AppBarService.AddComponent<ProductInfo>(25);
        
        ThemeService.CurrentThemeChanged += OnThemeChanged;
        ThemeService.IsDarkModeChanged += OnDarkModeChanged;
        AppBarService.AppBarItemsChanged += OnAppBarItemsChanged;
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState != null)
        {
            var authState = await AuthenticationState;
            if (authState.User.Identity?.IsAuthenticated == true && !authState.User.Claims.IsExpired())
            {
                await FeatureService.InitializeFeaturesAsync();
                StateHasChanged();
            }
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _errorBoundary?.Recover();
    }

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);
    private void OnDarkModeChanged() => InvokeAsync(StateHasChanged);
    private void OnAppBarItemsChanged() => InvokeAsync(StateHasChanged);

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    void IDisposable.Dispose()
    {
        ThemeService.CurrentThemeChanged -= OnThemeChanged;
        ThemeService.IsDarkModeChanged -= OnDarkModeChanged;
    }
}

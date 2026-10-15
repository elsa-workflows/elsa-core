using System.Reflection;
using Elsa.Studio.Contracts;
using Elsa.Studio.Shell.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Shell;

/// <summary>
/// Represents the app.
/// </summary>
public partial class App
{
    [Inject] private IEnumerable<IFeature> Modules { get; set; } = null!;

    [Inject] private IUnauthorizedComponentProvider UnauthorizedComponentProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    [Inject] private IOptions<ShellOptions> ShellOptions { get; set; } = null!;

    private IEnumerable<Assembly> AdditionalAssemblies { get; set; } = null!;

    private bool AuthorizationIsDisabled => ShellOptions.Value.DisableAuthorization;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        AdditionalAssemblies = Modules.Select(x => x.GetType().Assembly).Distinct().ToList();
    }
}
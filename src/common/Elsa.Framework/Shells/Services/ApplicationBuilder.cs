using Elsa.Framework.Shells;
using Elsa.Framework.Shells.Builders;
using Elsa.Framework.Shells.Extensions;
using Elsa.Framework.Shells.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Builders;

public class ApplicationBuilder
{
    public ApplicationBuilder()
    {
        ApplicationShell = AddShell();
    }

    private readonly ICollection<ShellBuilder> _shellBuilders = new List<ShellBuilder>();

    /// The application shell that is shared by tenants that are not isolated, and instead share the application shell.
    public ShellBuilder ApplicationShell { get; }

    public ShellBuilder AddShell()
    {
        var shellBuilder = new ShellBuilder(this);
        _shellBuilders.Add(shellBuilder);
        return shellBuilder;
    }

    public void Build(IServiceCollection services)
    {
        ValidateShells();

        foreach (var shellBuilder in _shellBuilders)
            shellBuilder.Build(services);

        services.AddSingleton<IElsaApplication>(sp => ActivatorUtilities.CreateInstance<ElsaApplication>(sp, ApplicationShell.Id));
        services.AddShells();
    }

    private void ValidateShells()
    {
        var hasDuplicateIds = HasDuplicateIds(_shellBuilders);
        if (hasDuplicateIds) throw new Exception("Shells must have unique IDs");
    }

    public static bool HasDuplicateIds(ICollection<ShellBuilder> shellBuilders)
    {
        var ids = new HashSet<string>();
        return shellBuilders.Any(obj => !ids.Add(obj.Id));
    }
}
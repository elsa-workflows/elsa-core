using Elsa.Framework.Builders;
using Elsa.Framework.Shells.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ElsaBuilderExtensions
{
    public static ShellBuilder AddShell(this ApplicationBuilder applicationBuilder, string id)
    {
        return applicationBuilder.AddShell(id, []);
    }

    public static ShellBuilder AddShell(this ApplicationBuilder applicationBuilder, params Type[] features)
    {
        return applicationBuilder.AddShell(null, features);
    }

    public static ShellBuilder AddShell(this ApplicationBuilder applicationBuilder, string? id, params Type[] features)
    {
        var shellBuilder = applicationBuilder.AddShell();
        if (id != null) shellBuilder.WithId(id);
        shellBuilder.AddFeatures(features);
        return shellBuilder;
    }
}
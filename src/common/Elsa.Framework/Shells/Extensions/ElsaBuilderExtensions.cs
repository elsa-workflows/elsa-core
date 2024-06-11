using Elsa.Framework.Builders;
using Elsa.Framework.Shells.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ElsaBuilderExtensions
{
    public static ShellBuilder AddShell(this ElsaBuilder elsaBuilder, string id)
    {
        return elsaBuilder.AddShell(id, []);
    }

    public static ShellBuilder AddShell(this ElsaBuilder elsaBuilder, params Type[] features)
    {
        return elsaBuilder.AddShell(null, features);
    }

    public static ShellBuilder AddShell(this ElsaBuilder elsaBuilder, string? id, params Type[] features)
    {
        var shellBuilder = elsaBuilder.AddShell();
        if (id != null) shellBuilder.WithId(id);
        shellBuilder.AddFeatures(features);
        return shellBuilder;
    }
}
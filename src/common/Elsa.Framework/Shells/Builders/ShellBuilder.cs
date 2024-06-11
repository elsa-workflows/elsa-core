using Elsa.Framework.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells.Builders;

public class ShellBuilder(ElsaBuilder elsaBuilder)
{
    private string _id = Guid.NewGuid().ToString();
    private readonly ISet<Type> _features = new HashSet<Type>();

    public string Id => _id;
    public ElsaBuilder ElsaBuilder { get; } = elsaBuilder;

    public ShellBuilder AddFeature<TFeature>() where TFeature : IShellFeature
    {
        _features.Add(typeof(TFeature));
        return this;
    }

    public ShellBuilder AddFeatures(IEnumerable<Type> features)
    {
        foreach (var feature in features)
            _features.Add(feature);

        return this;
    }

    public ShellBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public void Build(IServiceCollection services)
    {
        var shellBlueprint = new ShellBlueprint(_id, _features);
        services.AddSingleton(shellBlueprint);
    }
}
using Elsa.Framework.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells.Builders;

public class ShellBuilder(ElsaBuilder elsaBuilder) : IServiceConfigurator
{
    private string _id = Guid.NewGuid().ToString();
    private readonly ISet<Type> _features = new HashSet<Type>();

    public ElsaBuilder ElsaBuilder { get; } = elsaBuilder;

    public ShellBuilder AddFeature<TFeature>()
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
        ElsaBuilder.Build(services);
    }

    public void Configure(IServiceCollection services)
    {
        var shellBlueprint = new ShellBlueprint(_id, _features);
        services.AddSingleton(shellBlueprint);
    }
}
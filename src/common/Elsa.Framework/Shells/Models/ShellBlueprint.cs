namespace Elsa.Framework.Shells;

public class ShellBlueprint(string id, IEnumerable<Type> features)
{
    private readonly List<Type> _features = features.ToList();
    public string Id { get; } = id;
    public IReadOnlyCollection<Type> Features => _features.ToList();

    public void AddFeature<TFeature>()
    {
        AddFeature(typeof(TFeature));
    }

    public void AddFeatures(IEnumerable<Type> features)
    {
        _features.AddRange(features);
    }

    public void AddFeature(Type feature)
    {
        _features.Add(feature);
    }

    public void RemoveFeature<TFeature>()
    {
        RemoveFeature(typeof(TFeature));
    }

    public void RemoveFeature(Type feature)
    {
        _features.Remove(feature);
    }
}
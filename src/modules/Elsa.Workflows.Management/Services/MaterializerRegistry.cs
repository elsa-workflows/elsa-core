namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class MaterializerRegistry(Func<IEnumerable<IWorkflowMaterializer>> materializers) : IMaterializerRegistry
{
    private readonly Lazy<IReadOnlyCollection<IWorkflowMaterializer>> _materializers = new(() => materializers().ToArray());

    /// <inheritdoc />
    public IEnumerable<IWorkflowMaterializer> GetMaterializers() => _materializers.Value;

    /// <inheritdoc />
    public IWorkflowMaterializer? GetMaterializer(string name)
    {
        return _materializers.Value.FirstOrDefault(x => x.Name == name);
    }

    /// <inheritdoc />
    public bool IsMaterializerAvailable(string name)
    {
        return _materializers.Value.Any(x => x.Name == name);
    }
}

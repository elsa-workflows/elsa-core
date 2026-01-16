namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class MaterializerRegistry(Func<IEnumerable<IWorkflowMaterializer>> materializers) : IMaterializerRegistry
{
    /// <inheritdoc />
    public IEnumerable<IWorkflowMaterializer> GetMaterializers() => materializers();

    /// <inheritdoc />
    public IWorkflowMaterializer? GetMaterializer(string name)
    {
        return materializers().FirstOrDefault(x => x.Name == name);
    }

    /// <inheritdoc />
    public bool IsMaterializerAvailable(string name)
    {
        return materializers().Any(x => x.Name == name);
    }
}

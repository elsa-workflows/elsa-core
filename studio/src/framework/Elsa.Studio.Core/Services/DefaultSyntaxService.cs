using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultExpressionService : IExpressionService
{
    private readonly IExpressionProvider _expressionProvider;
    private ICollection<ExpressionDescriptor> _descriptors = default!;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExpressionService"/> class.
    /// </summary>
    public DefaultExpressionService(IExpressionProvider expressionProvider)
    {
        _expressionProvider = expressionProvider;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExpressionDescriptor>> ListDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        if (_descriptors == null!)
        {
            var descriptors = await _expressionProvider.ListAsync(cancellationToken);
            _descriptors = descriptors.ToList();
        }

        return _descriptors;
    }

    /// <inheritdoc />
    public async Task<ExpressionDescriptor?> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        var descriptors = await ListDescriptorsAsync(cancellationToken);
        return descriptors.FirstOrDefault(x => x.Type == type);
    }
}
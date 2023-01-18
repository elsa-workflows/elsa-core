using Elsa.JavaScript.Models;
using Elsa.JavaScript.Services;

namespace Elsa.JavaScript.Providers;

/// <summary>
/// Produces <see cref="FunctionDefinition"/>s for common functions.
/// </summary>
public class CommonFunctionsProvider : IFunctionDefinitionProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        var functions = new[]
        {
            new FunctionDefinition
            {
                Name = "setVariable",
                Parameters =
                {
                    new ParameterDefinition
                    {
                        Name = "name",
                        Type = "string"
                    },
                    new ParameterDefinition
                    {
                        Name = "value",
                        Type = "any"
                    }
                }
            }
        };

        return new(functions);
    }
}
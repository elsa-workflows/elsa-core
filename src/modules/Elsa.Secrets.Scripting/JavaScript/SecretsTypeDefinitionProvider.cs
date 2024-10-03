using Elsa.JavaScript.TypeDefinitions.Builders;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Secrets.Management;
using JetBrains.Annotations;

namespace Elsa.Secrets.Scripting.JavaScript;

[UsedImplicitly]
internal class SecretsTypeDefinitionProvider(ISecretManager secretManager) : ITypeDefinitionProvider, IVariableDefinitionProvider
{
    public async ValueTask<IEnumerable<TypeDefinition>> GetTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var filter = new SecretFilter
        {
            Status = SecretStatus.Active
        };
        var secrets = await secretManager.FindManyAsync(filter, cancellationToken);
        
        var secretsContainerClass = new TypeDefinition
        {
            Name = "SecretVariables",
            DeclarationKeyword = "class"
        };

        foreach (var secret in secrets)
        {
            secretsContainerClass.Properties.Add(new PropertyDefinition
            {
                Name = secret.Name,
                Type = "string"
            });
        }
        
        return [secretsContainerClass];
    }
    
    public ValueTask<IEnumerable<VariableDefinition>> GetVariableDefinitionsAsync(TypeDefinitionContext context)
    {
        var definitions = new List<VariableDefinition>
        {
            new VariableDefinitionBuilder().Name("secrets").Type("SecretVariables").Build()
        };
            
                
        return new (definitions);
    }
}
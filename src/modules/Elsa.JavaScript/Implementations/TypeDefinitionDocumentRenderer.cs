using System.Text;
using Elsa.JavaScript.Models;
using Elsa.JavaScript.Services;

namespace Elsa.JavaScript.Implementations;

/// <inheritdoc />
public class TypeDefinitionDocumentRenderer : ITypeDefinitionDocumentRenderer
{
    public string Render(TypeDefinitionsDocument document)
    {
        var stringBuilder = new StringBuilder();

        foreach (var functionDefinition in document.Functions) 
            Render(functionDefinition, stringBuilder);
        
        foreach (var typeDefinition in document.Types) 
            Render(typeDefinition, stringBuilder);

        return stringBuilder.ToString();
    }
    
    private void Render(FunctionDefinition functionDefinition, StringBuilder output)
    {
        // TODO.
    }
    
    private void Render(TypeDefinition typeDefinition, StringBuilder output)
    {
        output.AppendLine($"export {typeDefinition.DeclarationKeyword} {typeDefinition.Name} {{");

        foreach (var member in typeDefinition.Fields) 
            Render(member, output);

        output.AppendLine("}");
    }

    private void Render(FieldDefinition field, StringBuilder output)
    {
        output.AppendLine($"{field.Type}{(field.IsOptional ? "?" : "")} {field.Name}");
    }
}
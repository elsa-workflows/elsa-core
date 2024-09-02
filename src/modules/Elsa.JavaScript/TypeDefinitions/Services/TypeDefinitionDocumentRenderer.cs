using System.Text;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;

namespace Elsa.JavaScript.TypeDefinitions.Services;

/// <inheritdoc />
public class TypeDefinitionDocumentRenderer : ITypeDefinitionDocumentRenderer
{
    /// <inheritdoc />
    public string Render(TypeDefinitionsDocument document)
    {
        var stringBuilder = new StringBuilder();

        foreach (var functionDefinition in document.Functions)
            Render(functionDefinition, stringBuilder);

        foreach (var typeDefinition in document.Types)
            Render(typeDefinition, stringBuilder);

        foreach (var variableDefinition in document.Variables) 
            Render(variableDefinition, stringBuilder);

        return stringBuilder.ToString();
    }

    private void Render(FunctionDefinition functionDefinition, StringBuilder output)
    {
        string RenderParameter(ParameterDefinition parameter) => $"{parameter.Name}{(parameter.IsOptional ? "?" : "")}: {parameter.Type}";
        string RenderParameters(IEnumerable<ParameterDefinition> parameters) => string.Join(", ", parameters.Select(RenderParameter));
        
        var returnType = functionDefinition.ReturnType != null ? $": {functionDefinition.ReturnType}" : "";
        output.AppendLine($"declare function {functionDefinition.Name}({RenderParameters(functionDefinition.Parameters)}){returnType};");
    }

    private void Render(TypeDefinition typeDefinition, StringBuilder output)
    {
        output.AppendLine($"declare {typeDefinition.DeclarationKeyword} {typeDefinition.Name} {{");

        foreach (var property in typeDefinition.Properties)
            Render(property, output);

        output.AppendLine("}");
    }

    private void Render(PropertyDefinition property, StringBuilder output) => output.AppendLine($"{property.Name}{(property.IsOptional ? "?" : "")}: {property.Type};");
    
    private void Render(VariableDefinition variable, StringBuilder output) => output.AppendLine($"declare var {variable.Name}: {variable.Type};");
}
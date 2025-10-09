using System.Text;
using Elsa.Expressions.JavaScript.TypeDefinitions.Contracts;
using Elsa.Expressions.JavaScript.TypeDefinitions.Models;
namespace Elsa.Expressions.JavaScript.TypeDefinitions.Services;
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
        var returnType = functionDefinition.ReturnType != null ? $": {functionDefinition.ReturnType}" : "";
        output.AppendLine($"declare function {functionDefinition.Name}({RenderParameters(functionDefinition.Parameters)}){returnType};");
    }
    
    private void RenderMethod(FunctionDefinition functionDefinition, StringBuilder output)
    {
        var returnType = functionDefinition.ReturnType != null ? $" => {functionDefinition.ReturnType}" : "";
        output.AppendLine($"{functionDefinition.Name}: ({RenderParameters(functionDefinition.Parameters)}){returnType};");
    }
    private void Render(TypeDefinition typeDefinition, StringBuilder output)
    {
        output.AppendLine($"declare {typeDefinition.DeclarationKeyword} {typeDefinition.Name} {{");
        if (typeDefinition.DeclarationKeyword == "enum")
        {
            foreach (var property in typeDefinition.Properties)
                RenderEnumMember(property, output);
        }
        else
        {
            foreach (var property in typeDefinition.Properties)
                Render(property, output);
        }
        foreach (var method in typeDefinition.Methods)
            RenderMethod(method, output);
        output.AppendLine("}");
    }

    private void Render(PropertyDefinition property, StringBuilder output) => output.AppendLine($"{property.Name}{(property.IsOptional ? "?" : "")}: {property.Type};");
    private void RenderEnumMember(PropertyDefinition property, StringBuilder output) => output.AppendLine($"{property.Name} = \"{property.Name}\",");
    private void Render(VariableDefinition variable, StringBuilder output) => output.AppendLine($"declare var {variable.Name}: {variable.Type};");
    string RenderParameter(ParameterDefinition parameter) => $"{parameter.Name}{(parameter.IsOptional ? "?" : "")}: {parameter.Type}";
    string RenderParameters(IEnumerable<ParameterDefinition> parameters) => string.Join(", ", parameters.Select(RenderParameter));
}
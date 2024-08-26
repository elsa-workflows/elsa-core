using Elsa.Extensions;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.TypeDefinitions.Abstractions;
using Elsa.JavaScript.TypeDefinitions.Models;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.JavaScript.TypeDefinitions.Providers;

/// Produces <see cref="FunctionDefinition"/>s for common functions.
[UsedImplicitly]
internal class CommonFunctionsDefinitionProvider(ITypeAliasRegistry typeAliasRegistry) : FunctionDefinitionProvider
{
    protected override IEnumerable<FunctionDefinition> GetFunctionDefinitions(TypeDefinitionContext context)
    {
        yield return CreateFunctionDefinition(builder => builder
            .Name("getWorkflowDefinitionId")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getWorkflowDefinitionVersionId")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getWorkflowDefinitionVersion")
            .ReturnType("number"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getWorkflowInstanceId")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getCorrelationId")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("setCorrelationId")
            .Parameter("value", "string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("setVariable")
            .Parameter("name", "string")
            .Parameter("value", "any"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getVariable")
            .Parameter("name", "string")
            .ReturnType("any"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getInput")
            .Parameter("name", "string")
            .ReturnType("any"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getOutputFrom")
            .Parameter("activityId", "string")
            .Parameter("outputName", "string", true)
            .ReturnType("any"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getLastResult")
            .ReturnType("any"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("isNullOrWhiteSpace")
            .Parameter("value", "string")
            .ReturnType("boolean"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("isNullOrEmpty")
            .Parameter("value", "string")
            .ReturnType("boolean"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("parseGuid")
            .Parameter("value", "string")
            .ReturnType("Guid"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getShortGuid")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getGuid")
            .ReturnType("Guid"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("getGuidString")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("toJson")
            .Parameter("value", "any")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("bytesToString")
            .Parameter("value", "Byte[]")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("bytesFromString")
            .Parameter("value", "string")
            .ReturnType("Byte[]"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("bytesToBase64")
            .Parameter("value", "Byte[]")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("bytesFromBase64")
            .Parameter("value", "string")
            .ReturnType("Byte[]"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("stringFromBase64")
            .Parameter("value", "string")
            .ReturnType("string"));

        yield return CreateFunctionDefinition(builder => builder
            .Name("stringToBase64")
            .Parameter("value", "string")
            .ReturnType("string"));

        // Variable getter and setters.
        foreach (var variable in context.WorkflowGraph.Workflow.Variables)
        {
            var pascalName = variable.Name.Pascalize();
            var variableType = variable.GetVariableType();
            var typeAlias = typeAliasRegistry.TryGetAlias(variableType, out var alias) ? alias : "any";

            // get{Variable}.
            yield return CreateFunctionDefinition(builder => builder.Name($"get{pascalName}").ReturnType(typeAlias));

            // set{Variable}.
            yield return CreateFunctionDefinition(builder => builder.Name($"set{pascalName}").Parameter("value", typeAlias));
        }
    }
}
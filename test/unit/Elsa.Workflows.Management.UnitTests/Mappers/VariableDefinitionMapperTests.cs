using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Mappers;

public class VariableDefinitionMapperTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly WorkflowJsonOptions _workflowJsonOptions = new();
    private readonly VariableDefinitionMapper _mapper;

    public VariableDefinitionMapperTests()
    {
        _workflowJsonOptions.RegisterTypeAlias(typeof(string), "String");
        _workflowJsonOptions.RegisterTypeAlias(typeof(MemoryStorageDriver), nameof(MemoryStorageDriver));
        _mapper = CreateMapper(_workflowJsonOptions);
    }

    [Fact]
    public void Map_ResolvesUnregisteredClrTypeName_WhenLegacyClrTypeNamesEnabled()
    {
        var definition = new VariableDefinition("id", "payload", typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName(), false, null, null);

        var variable = _mapper.Map(definition);

        Assert.IsType<Variable<UnregisteredPayload>>(variable);
    }

    [Fact]
    public void Map_DoesNotResolveUnregisteredClrTypeName_WhenLegacyClrTypeNamesDisabled()
    {
        var workflowJsonOptions = new WorkflowJsonOptions
        {
            AllowLegacyClrTypeNames = false
        };
        var mapper = CreateMapper(workflowJsonOptions);
        var definition = new VariableDefinition("id", "payload", typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName(), false, null, null);

        var variable = mapper.Map(definition);

        Assert.Null(variable);
    }

    [Fact]
    public void Map_WritesUnregisteredVariableTypeAsClrTypeName()
    {
        var variable = new Variable<UnregisteredPayload>("payload", new());

        var definition = _mapper.Map(variable);

        Assert.Equal(typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName(), definition.TypeName);
    }

    [Fact]
    public void Map_RejectsUnregisteredVariableType_WhenLegacyClrTypeNamesDisabled()
    {
        var workflowJsonOptions = new WorkflowJsonOptions
        {
            AllowLegacyClrTypeNames = false
        };
        var mapper = CreateMapper(workflowJsonOptions);
        var variable = new Variable<UnregisteredPayload>("payload", new());

        Assert.Throws<JsonException>(() => mapper.Map(variable));
    }

    private VariableDefinitionMapper CreateMapper(WorkflowJsonOptions workflowJsonOptions)
    {
        return new(Microsoft.Extensions.Options.Options.Create(workflowJsonOptions), _scopeFactory, NullLogger<VariableDefinitionMapper>.Instance);
    }

    private sealed class UnregisteredPayload
    {
    }
}

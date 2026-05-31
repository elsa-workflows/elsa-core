using Elsa.Extensions;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Mappers;

public class VariableDefinitionMapperTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly WorkflowJsonTypeOptions _workflowJsonTypeOptions = new();
    private readonly VariableDefinitionMapper _mapper;

    public VariableDefinitionMapperTests()
    {
        _workflowJsonTypeOptions.RegisterTypeAlias(typeof(string), "String");
        _workflowJsonTypeOptions.RegisterTypeAlias(typeof(MemoryStorageDriver), nameof(MemoryStorageDriver));
        _mapper = CreateMapper(_workflowJsonTypeOptions);
    }

    [Fact]
    public void Map_DoesNotResolveUnregisteredClrTypeName()
    {
        var definition = new VariableDefinition("id", "payload", typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName(), false, null, null);

        var variable = _mapper.Map(definition);

        Assert.Null(variable);
    }

    [Fact]
    public void Map_ResolvesRegisteredLegacyClrTypeName()
    {
        var workflowJsonTypeOptions = new WorkflowJsonTypeOptions();
        workflowJsonTypeOptions.RegisterTypeAlias(typeof(UnregisteredPayload), nameof(UnregisteredPayload));
        workflowJsonTypeOptions.RegisterLegacySimpleAssemblyQualifiedName(typeof(UnregisteredPayload));
        var mapper = CreateMapper(workflowJsonTypeOptions);
        var definition = new VariableDefinition("id", "payload", typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName(), false, null, null);

        var variable = mapper.Map(definition);

        Assert.IsType<Variable<UnregisteredPayload>>(variable);
    }

    [Fact]
    public void Map_WritesUnregisteredVariableTypeAsClrTypeName()
    {
        var variable = new Variable<UnregisteredPayload>("payload", new());

        var definition = _mapper.Map(variable);

        Assert.Equal(typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName(), definition.TypeName);
    }

    [Fact]
    public void Map_WritesRegisteredVariableTypeAsAlias()
    {
        var workflowJsonTypeOptions = new WorkflowJsonTypeOptions();
        workflowJsonTypeOptions.RegisterTypeAlias(typeof(UnregisteredPayload), nameof(UnregisteredPayload));
        var mapper = CreateMapper(workflowJsonTypeOptions);
        var variable = new Variable<UnregisteredPayload>("payload", new());

        var definition = mapper.Map(variable);

        Assert.Equal(nameof(UnregisteredPayload), definition.TypeName);
    }

    private VariableDefinitionMapper CreateMapper(WorkflowJsonTypeOptions workflowJsonTypeOptions)
    {
        var workflowJsonTypeRegistry = new WorkflowJsonTypeRegistry(Microsoft.Extensions.Options.Options.Create(workflowJsonTypeOptions));
        return new(workflowJsonTypeRegistry, _scopeFactory, NullLogger<VariableDefinitionMapper>.Instance);
    }

    private sealed class UnregisteredPayload
    {
    }
}

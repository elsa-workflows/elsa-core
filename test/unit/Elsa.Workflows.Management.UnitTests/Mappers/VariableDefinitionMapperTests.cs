using Elsa.Extensions;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Elsa.Common.Serialization;

namespace Elsa.Workflows.Management.UnitTests.Mappers;

public class VariableDefinitionMapperTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly SerializationTypeOptions _workflowJsonTypeOptions = new();
    private readonly VariableDefinitionMapper _mapper;

    public VariableDefinitionMapperTests()
    {
        _workflowJsonTypeOptions.RegisterTypeAlias(typeof(string), "String");
        _workflowJsonTypeOptions.RegisterTypeAlias(typeof(MemoryStorageDriver), nameof(MemoryStorageDriver));
        _mapper = CreateMapper(_workflowJsonTypeOptions);
    }

    [Test]
    public async Task Map_DoesNotResolveUnregisteredClrTypeName()
    {
        var definition = new VariableDefinition("id", "payload", typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName(), false, null, null);

        var variable = _mapper.Map(definition);

        await Assert.That(variable).IsNull();
    }

    [Test]
    public async Task Map_ResolvesRegisteredLegacyClrTypeName()
    {
        var workflowJsonTypeOptions = new SerializationTypeOptions();
        workflowJsonTypeOptions.RegisterTypeAlias(typeof(UnregisteredPayload), nameof(UnregisteredPayload));
        workflowJsonTypeOptions.RegisterLegacySimpleAssemblyQualifiedName(typeof(UnregisteredPayload));
        var mapper = CreateMapper(workflowJsonTypeOptions);
        var definition = new VariableDefinition("id", "payload", typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName(), false, null, null);

        var variable = mapper.Map(definition);

        await Assert.That(variable).IsOfType(typeof(Variable<UnregisteredPayload>));
        _ = (Variable<UnregisteredPayload>)variable!;
    }

    [Test]
    public async Task Map_WritesUnregisteredVariableTypeAsClrTypeName()
    {
        var variable = new Variable<UnregisteredPayload>("payload", new());

        var definition = _mapper.Map(variable);

        await Assert.That(definition.TypeName).IsEqualTo(typeof(UnregisteredPayload).GetSimpleAssemblyQualifiedName());
    }

    [Test]
    public async Task Map_WritesRegisteredVariableTypeAsAlias()
    {
        var workflowJsonTypeOptions = new SerializationTypeOptions();
        workflowJsonTypeOptions.RegisterTypeAlias(typeof(UnregisteredPayload), nameof(UnregisteredPayload));
        var mapper = CreateMapper(workflowJsonTypeOptions);
        var variable = new Variable<UnregisteredPayload>("payload", new());

        var definition = mapper.Map(variable);

        await Assert.That(definition.TypeName).IsEqualTo(nameof(UnregisteredPayload));
    }

    private VariableDefinitionMapper CreateMapper(SerializationTypeOptions workflowJsonTypeOptions)
    {
        var workflowJsonTypeRegistry = new SerializationTypeRegistry(Microsoft.Extensions.Options.Options.Create(workflowJsonTypeOptions));
        return new(workflowJsonTypeRegistry, _scopeFactory, NullLogger<VariableDefinitionMapper>.Instance);
    }

    private sealed class UnregisteredPayload
    {
    }
}

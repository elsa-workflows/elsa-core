using Elsa.Expressions.Services;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class VariableMapperTests
{
    private readonly WellKnownTypeRegistry _registry = new();
    private readonly VariableMapper _mapper;

    public VariableMapperTests()
    {
        _registry.RegisterType(typeof(string), "String");
        _registry.RegisterType(typeof(WorkflowStorageDriver), nameof(WorkflowStorageDriver));
        _mapper = new(_registry, NullLogger<VariableMapper>.Instance);
    }

    [Fact]
    public void Map_ResolvesRegisteredVariableTypeAlias()
    {
        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", null));

        Assert.IsType<Variable<string>>(variable);
    }

    [Fact]
    public void Map_ResolvesRegisteredStorageDriverAlias()
    {
        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", nameof(WorkflowStorageDriver)));

        Assert.Equal(typeof(WorkflowStorageDriver), variable.StorageDriverType);
    }

    [Fact]
    public void Map_DoesNotLoadUnregisteredStorageDriverAssemblyQualifiedName()
    {
        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", typeof(VariableMapperTests).AssemblyQualifiedName));

        Assert.Null(variable.StorageDriverType);
    }
}

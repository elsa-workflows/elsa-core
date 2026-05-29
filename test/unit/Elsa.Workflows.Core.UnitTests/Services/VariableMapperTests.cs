using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Options;
using Elsa.Workflows.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class VariableMapperTests
{
    private readonly WorkflowJsonOptions _workflowJsonOptions = new();
    private readonly VariableMapper _mapper;

    public VariableMapperTests()
    {
        _workflowJsonOptions.RegisterTypeAlias(typeof(string), "String");
        _workflowJsonOptions.RegisterTypeAlias(typeof(WorkflowStorageDriver), nameof(WorkflowStorageDriver));
        _workflowJsonOptions.RegisterTypeAlias(typeof(MemoryStorageDriver), typeof(MemoryStorageDriver).GetSimpleAssemblyQualifiedName());
        _mapper = new(NullLogger<VariableMapper>.Instance, _workflowJsonOptions);
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
    public void Map_ResolvesRegisteredStorageDriverAssemblyQualifiedName()
    {
        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", typeof(MemoryStorageDriver).GetSimpleAssemblyQualifiedName()));

        Assert.Equal(typeof(MemoryStorageDriver), variable.StorageDriverType);
    }

    [Fact]
    public void Map_DoesNotLoadUnregisteredStorageDriverAssemblyQualifiedName()
    {
        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", typeof(VariableMapperTests).AssemblyQualifiedName));

        Assert.Null(variable.StorageDriverType);
    }

    [Fact]
    public void Map_DoesNotUseRegisteredNonStorageDriverAliasAsStorageDriver()
    {
        _workflowJsonOptions.RegisterTypeAlias(typeof(string), "NotAStorageDriver");

        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", "NotAStorageDriver"));

        Assert.Null(variable.StorageDriverType);
    }
}

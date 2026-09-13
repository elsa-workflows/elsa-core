using System.Threading.Tasks;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class VariableMapperTests
{
    private readonly SerializationTypeRegistry _registry = new(Microsoft.Extensions.Options.Options.Create(new SerializationTypeOptions()));
    private readonly VariableMapper _mapper;

    public VariableMapperTests()
    {
        _registry.RegisterType(typeof(string), "String");
        _registry.RegisterType(typeof(WorkflowStorageDriver), nameof(WorkflowStorageDriver));
        _registry.RegisterType(typeof(MemoryStorageDriver), typeof(MemoryStorageDriver).GetSimpleAssemblyQualifiedName());
        _mapper = new(_registry, NullLogger<VariableMapper>.Instance);
    }

    [Test]
    public async Task Map_ResolvesRegisteredVariableTypeAlias()
    {
        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", null));

        await Assert.That(variable).IsTypeOf<Variable<string>>();
    }

    [Test]
    public async Task Map_ResolvesRegisteredStorageDriverAlias()
    {
        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", nameof(WorkflowStorageDriver)));

        await Assert.That(variable.StorageDriverType).IsEqualTo(typeof(WorkflowStorageDriver));
    }

    [Test]
    public async Task Map_ResolvesRegisteredMemoryStorageDriverAssemblyQualifiedName()
    {
        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", typeof(MemoryStorageDriver).GetSimpleAssemblyQualifiedName()));

        await Assert.That(variable.StorageDriverType).IsEqualTo(typeof(MemoryStorageDriver));
    }

    [Test]
    public async Task Map_WritesRegisteredStorageDriverAlias()
    {
        var model = _mapper.Map(new Variable<string>("name", "") { StorageDriverType = typeof(WorkflowStorageDriver) });

        await Assert.That(model.StorageDriverTypeName).IsEqualTo(nameof(WorkflowStorageDriver));
    }

    [Test]
    public async Task Map_DoesNotLoadUnregisteredStorageDriverAssemblyQualifiedName()
    {
        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", typeof(VariableMapperTests).AssemblyQualifiedName));

        await Assert.That(variable.StorageDriverType).IsNull();
    }

    [Test]
    public async Task Map_DoesNotUseRegisteredNonStorageDriverAliasAsStorageDriver()
    {
        _registry.RegisterType(typeof(string), "NotAStorageDriver");

        var variable = _mapper.Map(new VariableModel("id", "name", "String", "value", "NotAStorageDriver"));

        await Assert.That(variable.StorageDriverType).IsNull();
    }
}
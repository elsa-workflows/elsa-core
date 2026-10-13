using Elsa.Common.Services;
using Elsa.Labels.Endpoints.Labels;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Labels.UnitTests.Endpoints;

public class LabelNameValidatorTests
{
    [Theory]
    [InlineData(255, false)]
    [InlineData(256, true)]
    public void IsTooLong_UsesNameMaxLength(int length, bool expected)
    {
        Assert.Equal(expected, LabelNameValidator.IsTooLong(new string('a', length)));
    }

    [Fact]
    public async Task IsDuplicateAsync_WhenSameNameInDifferentCase_ReturnsTrue()
    {
        var store = await CreateStoreWithLabelAsync("label-1", "Foo");

        Assert.True(await LabelNameValidator.IsDuplicateAsync(store, " FOO ", null, CancellationToken.None));
    }

    [Fact]
    public async Task IsDuplicateAsync_WhenRenamingLabelToItsOwnName_ReturnsFalse()
    {
        var store = await CreateStoreWithLabelAsync("label-1", "Foo");

        Assert.False(await LabelNameValidator.IsDuplicateAsync(store, "foo", "label-1", CancellationToken.None));
    }

    private static async Task<InMemoryLabelStore> CreateStoreWithLabelAsync(string id, string name)
    {
        var store = new InMemoryLabelStore(new MemoryStore<Label>(), new MemoryStore<WorkflowDefinitionLabel>(), new TestTenantAccessor("tenant-a"));
        await store.SaveAsync(new Label { Id = id, Name = name, TenantId = "tenant-a" });
        return store;
    }
}

using System.Text.Json;
using Elsa.Common.Entities;

namespace Elsa.Common.UnitTests.Entities;

public class OrderDefinitionTests
{
    [Fact]
    public void Serialize_DoesNotIncludeKeySelectorText()
    {
        var order = new OrderDefinition<TestEntity, string>(x => x.Name, OrderDirection.Ascending);

        var json = JsonSerializer.Serialize(order);

        Assert.DoesNotContain(nameof(OrderDefinition<TestEntity, string>.KeySelectorText), json);
    }

    private sealed class TestEntity
    {
        public string Name { get; set; } = null!;
    }
}

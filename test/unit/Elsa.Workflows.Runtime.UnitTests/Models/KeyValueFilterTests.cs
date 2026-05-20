using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;

namespace Elsa.Workflows.Runtime.UnitTests.Models;

public class KeyValueFilterTests
{
    [Fact]
    public void Apply_OrdersByPersistedKey_WhenOrderByKeyIsEnabled()
    {
        var filter = new KeyValueFilter { OrderByKey = true };

        var result = filter.Apply(CreateUnorderedPairs()).Select(x => x.Key).ToList();

        Assert.Equal(["key-a", "key-b", "key-c"], result);
    }

    [Fact]
    public void Apply_AppliesTakeAfterOrderingByPersistedKey()
    {
        var filter = new KeyValueFilter { OrderByKey = true, Take = 2 };

        var result = filter.Apply(CreateUnorderedPairs()).Select(x => x.Key).ToList();

        Assert.Equal(["key-a", "key-b"], result);
    }

    private static IQueryable<SerializedKeyValuePair> CreateUnorderedPairs()
    {
        return new[]
        {
            new SerializedKeyValuePair { Key = "key-c", SerializedValue = "c" },
            new SerializedKeyValuePair { Key = "key-a", SerializedValue = "a" },
            new SerializedKeyValuePair { Key = "key-b", SerializedValue = "b" }
        }.AsQueryable();
    }
}

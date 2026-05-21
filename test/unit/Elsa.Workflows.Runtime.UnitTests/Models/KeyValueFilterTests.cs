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

    [Fact]
    public void Apply_ReturnsNoRecords_WhenTakeIsZero()
    {
        var filter = new KeyValueFilter { Take = 0 };

        var result = filter.Apply(CreateUnorderedPairs()).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Apply_DoesNotLimitRecords_WhenTakeIsNull()
    {
        var filter = new KeyValueFilter { Take = null };

        var result = filter.Apply(CreateUnorderedPairs()).Select(x => x.Key).ToList();

        Assert.Equal(["key-c", "key-a", "key-b"], result);
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

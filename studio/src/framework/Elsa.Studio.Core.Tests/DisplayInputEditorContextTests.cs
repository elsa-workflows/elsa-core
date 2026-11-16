using Elsa.Studio.Models;
using Xunit;

namespace Elsa.Studio.Core.Tests;

public class DisplayInputEditorContextTests
{
    [Fact]
    public async Task UpdateValueAsync_UpdatesValue()
    {
        var context = new DisplayInputEditorContext
        {
            Value = "old",
            OnValueChanged = _ => Task.CompletedTask
        };

        await context.UpdateValueAsync("new");

        Assert.Equal("new", context.Value);
    }

    [Fact]
    public async Task UpdateValueAsync_UpdatesValueBeforeNotifying()
    {
        object? valueSeenByHandler = null;
        var context = new DisplayInputEditorContext { Value = "old" };
        context.OnValueChanged = _ =>
        {
            valueSeenByHandler = context.Value;
            return Task.CompletedTask;
        };

        await context.UpdateValueAsync("new");

        Assert.Equal("new", valueSeenByHandler);
    }
}

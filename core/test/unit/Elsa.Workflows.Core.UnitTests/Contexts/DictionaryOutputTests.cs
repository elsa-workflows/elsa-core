using Elsa.Testing.Shared;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Xunit;

namespace Elsa.Workflows.Core.UnitTests.Contexts;

public class DictionaryOutputTests
{
    [Fact]
    public async Task Set_RecordsEntryUnderCompositeNameAndInItsMemoryBlock()
    {
        var activity = new ActivityWithDictionaryOutputs("name");

        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        var register = context.WorkflowExecutionContext.GetActivityOutputRegister();
        Assert.Equal("Frank", register.FindOutputByActivityId(activity.Id, "Outputs:name"));
        Assert.Null(register.FindOutputByActivityId(activity.Id, "name"));
        Assert.Equal("Frank", context.Get(activity.Outputs["name"]));
    }

    [Fact]
    public async Task Set_UsesTheOutputNameOfTheDescriptor()
    {
        var activity = new ActivityWithDictionaryOutputs("name");

        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        var register = context.WorkflowExecutionContext.GetActivityOutputRegister();
        Assert.Equal("Frank", register.FindOutputByActivityId(activity.Id, "Renamed:name"));
    }

    [Fact]
    public async Task Set_WhenKeyHasNoEntry_Throws()
    {
        var activity = new ActivityWithDictionaryOutputs("missing");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => new ActivityTestFixture(activity).ExecuteAsync());

        Assert.Contains("'Outputs' has no entry with key 'missing'", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("first:second")]
    public async Task Set_WhenKeyIsInvalid_Throws(string key)
    {
        var activity = new ActivityWithDictionaryOutputs(key);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => new ActivityTestFixture(activity).ExecuteAsync());

        Assert.Contains($"Key '{key}' of output 'Outputs'", exception.Message);
    }

    private sealed class ActivityWithDictionaryOutputs(string keyToSet) : CodeActivity
    {
        public Dictionary<string, Output<object>> Outputs { get; set; } = new() { ["name"] = new() };

        [Output(Name = "Renamed")]
        public Dictionary<string, Output<object>> RenamedOutputs { get; set; } = new() { ["name"] = new() };

        protected override void Execute(ActivityExecutionContext context)
        {
            context.Set(Outputs, keyToSet, "Frank");
            context.Set(RenamedOutputs, "name", "Frank");
        }
    }
}

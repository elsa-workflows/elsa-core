using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.Dropdown;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Serialization.UIHintSerializiation;

public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationTests"/> class.
    /// </summary>
    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa =>
            {
                elsa.AddActivity<TestActivity>();
            })
            .Build();
    }

    [Fact(DisplayName = "Enum input types get a dropdown UIHint by default")]
    public async Task Test1()
    {
        var activityDescriber = _services.GetRequiredService<IActivityDescriber>();

        var description = await activityDescriber.DescribeActivityAsync(typeof(TestActivity));

        var inputDescription = description.Inputs.First();
        Assert.Equal(InputUIHints.DropDown, inputDescription.UIHint);
    }

    [Fact(DisplayName = "Enum input types get a dropdown UIHint by default")]
    public async Task Test2()
    {
        var activityDescriber = _services.GetRequiredService<IActivityDescriber>();

        var description = await activityDescriber.DescribeActivityAsync(typeof(TestActivity));

        var inputDescription = description.Inputs.First();
        Assert.True(inputDescription.UISpecifications!.ContainsKey(InputUIHints.DropDown));
        Assert.True(inputDescription.UISpecifications[InputUIHints.DropDown] is DropDownProps);
        var dropDownProperties = (DropDownProps) inputDescription.UISpecifications[InputUIHints.DropDown];

        Assert.Collection(dropDownProperties.SelectList!.Items,
            item => { Assert.Equal("OptionsAreNice", item.Text); Assert.Equal("OptionsAreNice", item.Value); },
            item => { Assert.Equal("ToHave", item.Text); Assert.Equal("ToHave", item.Value); },
            item => { Assert.Equal("IfYouCanChooseThem", item.Text); Assert.Equal("IfYouCanChooseThem", item.Value); });
    }

}

using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.Dropdown;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Serialization.UIHintSerializiation;

public class Tests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationTests"/> class.
    /// </summary>
    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa =>
            {
                elsa.AddActivity<TestActivity>();
            })
            .Build();
    }

    [Test]
    [DisplayName("Enum input types get a dropdown UIHint by default")]
    public async Task Test1()
    {
        var activityDescriber = _services.GetRequiredService<IActivityDescriber>();

        var description = await activityDescriber.DescribeActivityAsync(typeof(TestActivity));

        var inputDescription = description.Inputs.First();
        await Assert.That(inputDescription.UIHint).IsEqualTo(InputUIHints.DropDown);
    }

    [Test]
    [DisplayName("Enum input types get a dropdown UIHint by default")]
    public async Task Test2()
    {
        var activityDescriber = _services.GetRequiredService<IActivityDescriber>();

        var description = await activityDescriber.DescribeActivityAsync(typeof(TestActivity));

        var inputDescription = description.Inputs.First();
        await Assert.That(inputDescription.UISpecifications!.ContainsKey(InputUIHints.DropDown)).IsTrue();
        await Assert.That(inputDescription.UISpecifications[InputUIHints.DropDown] is DropDownProps).IsTrue();
        var dropDownProperties = (DropDownProps) inputDescription.UISpecifications[InputUIHints.DropDown];

        var items = dropDownProperties.SelectList!.Items.ToList();
        await Assert.That(items.Count).IsEqualTo(3);
        await Assert.That(items[0].Text).IsEqualTo("OptionsAreNice");
        await Assert.That(items[0].Value).IsEqualTo("OptionsAreNice");
        await Assert.That(items[1].Text).IsEqualTo("ToHave");
        await Assert.That(items[1].Value).IsEqualTo("ToHave");
        await Assert.That(items[2].Text).IsEqualTo("IfYouCanChooseThem");
        await Assert.That(items[2].Value).IsEqualTo("IfYouCanChooseThem");
    }


    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}

using Elsa.Labels.Entities;

namespace Elsa.Labels.UnitTests.Entities;

public class LabelNameMaxLengthTests
{
    [Test]
    public async Task Name_WhenLongerThanSharedMaximum_Throws()
    {
        var label = new Label { Id = "label-1" };

        var exception = await Assert.That(() => label.Name = new string('a', Label.NameMaxLength + 1)).ThrowsExactly<ArgumentException>();

        await Assert.That(exception.ParamName).IsEqualTo("Name");
        await Assert.That(exception.Message).Contains(Label.NameMaxLength.ToString());
    }

    [Test]
    public async Task Name_WhenAtSharedMaximum_Succeeds()
    {
        var name = new string('a', Label.NameMaxLength);
        var label = new Label { Id = "label-1", Name = name };

        await Assert.That(label.Name).IsEqualTo(name);
        await Assert.That(label.NormalizedName).IsEqualTo(name);
    }
}

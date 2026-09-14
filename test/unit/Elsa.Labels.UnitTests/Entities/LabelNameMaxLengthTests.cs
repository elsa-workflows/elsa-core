using Elsa.Labels.Entities;

namespace Elsa.Labels.UnitTests.Entities;

public class LabelNameMaxLengthTests
{
    [Fact]
    public void Name_WhenLongerThanSharedMaximum_Throws()
    {
        var label = new Label { Id = "label-1" };

        var exception = Assert.Throws<ArgumentException>(() => label.Name = new string('a', Label.NameMaxLength + 1));

        Assert.Equal("Name", exception.ParamName);
        Assert.Contains(Label.NameMaxLength.ToString(), exception.Message);
    }

    [Fact]
    public void Name_WhenAtSharedMaximum_Succeeds()
    {
        var name = new string('a', Label.NameMaxLength);
        var label = new Label { Id = "label-1", Name = name };

        Assert.Equal(name, label.Name);
        Assert.Equal(name, label.NormalizedName);
    }
}

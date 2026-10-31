using Elsa.Common.Models;

namespace Elsa.Common.UnitTests.Models;

public class PageArgsTests
{
    [Fact]
    public void Next_FromPage_AdvancesByPageSize()
    {
        // Arrange
        var pageArgs = PageArgs.FromPage(0, 10);

        // Act
        var nextPage = pageArgs.Next();
        var thirdPage = nextPage.Next();

        // Assert
        Assert.Equal(10, nextPage.Offset);
        Assert.Equal(10, nextPage.Limit);
        Assert.Equal(1, nextPage.Page);
        Assert.Equal(20, thirdPage.Offset);
        Assert.Equal(2, thirdPage.Page);
    }

    [Fact]
    public void Next_AllPages_RemainsUnbounded()
    {
        // Arrange
        var pageArgs = PageArgs.All;

        // Act
        var nextPage = pageArgs.Next();

        // Assert
        Assert.Null(nextPage.Offset);
        Assert.Null(nextPage.Limit);
        Assert.Null(nextPage.Page);
    }
}
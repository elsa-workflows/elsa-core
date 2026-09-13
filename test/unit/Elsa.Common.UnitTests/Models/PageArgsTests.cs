using Elsa.Common.Models;
using System.Threading.Tasks;

namespace Elsa.Common.UnitTests.Models;

public class PageArgsTests
{
    [Test]
    public async Task Next_FromPage_AdvancesByPageSize()
    {
        // Arrange
        var pageArgs = PageArgs.FromPage(0, 10);

        // Act
        var nextPage = pageArgs.Next();
        var thirdPage = nextPage.Next();

        // Assert
        await Assert.That(nextPage.Offset).IsEqualTo(10);
        await Assert.That(nextPage.Limit).IsEqualTo(10);
        await Assert.That(nextPage.Page).IsEqualTo(1);
        await Assert.That(thirdPage.Offset).IsEqualTo(20);
        await Assert.That(thirdPage.Page).IsEqualTo(2);
    }

    [Test]
    public async Task Next_AllPages_RemainsUnbounded()
    {
        // Arrange
        var pageArgs = PageArgs.All;

        // Act
        var nextPage = pageArgs.Next();

        // Assert
        await Assert.That(nextPage.Offset).IsNull();
        await Assert.That(nextPage.Limit).IsNull();
        await Assert.That(nextPage.Page).IsNull();
    }
}
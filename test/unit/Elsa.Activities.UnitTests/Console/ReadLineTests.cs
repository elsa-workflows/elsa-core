using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Console;

/// <summary>
/// Unit tests for the <see cref="ReadLine"/> activity.
/// </summary>
public class ReadLineTests
{
    private readonly TextReader _mockTextReader;
    private readonly IStandardInStreamProvider _mockProvider;

    public ReadLineTests()
    {
        _mockTextReader = Substitute.For<TextReader>();
        _mockProvider = Substitute.For<IStandardInStreamProvider>();
        _mockProvider.GetTextReader().Returns(_mockTextReader);
    }

    [Theory(DisplayName = "ReadLine reads text from input")]
    [InlineData("Hello, World!")]
    [InlineData("Test input")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task Should_Read_Text_From_Input(string inputText)
    {
        // Arrange
        var readLine = new ReadLine();

        // Act
        var context = await ExecuteAsync(readLine, inputText);

        // Assert
        var result = (string)context.GetActivityOutput(() => readLine.Result)!;
        Assert.Equal(inputText, result);
    }

    [Fact(DisplayName = "ReadLine completes successfully")]
    public async Task Should_Complete_Successfully()
    {
        // Arrange
        var readLine = new ReadLine();

        // Act
        var context = await ExecuteAsync(readLine);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }

    [Fact(DisplayName = "ReadLine uses provided stream provider")]
    public async Task Should_Use_Provided_Stream_Provider()
    {
        // Arrange
        var readLine = new ReadLine();

        // Act
        await ExecuteAsync(readLine);

        // Assert
        _mockProvider.Received(1).GetTextReader();
        _mockTextReader.Received(1).ReadLine();
    }

    private async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity, string inputText = "Test")
    {
        _mockTextReader.ReadLine().Returns(inputText);
        return await new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton(_mockProvider))
            .ExecuteAsync();
    }
}

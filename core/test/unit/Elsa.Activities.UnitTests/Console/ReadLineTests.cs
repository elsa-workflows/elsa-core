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

    [Theory(DisplayName = "ReadLine reads text from input and completes successfully")]
    [InlineData("Hello, World!")]
    [InlineData("Test input")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task Should_Read_Text_From_Input_And_Complete(string inputText)
    {
        // Arrange
        var readLine = new ReadLine();

        // Act
        var context = await ExecuteAsync(readLine, inputText);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        var result = (string)context.GetActivityOutput(() => readLine.Result)!;
        Assert.Equal(inputText, result);
    }

    [Fact(DisplayName = "ReadLine uses provided stream provider")]
    public async Task Should_Use_Provided_Stream_Provider()
    {
        // Act
        await ExecuteAsync(new ReadLine());

        // Assert
        _mockProvider.Received(1).GetTextReader();
        _mockTextReader.Received(1).ReadLine();
    }

    [Fact(DisplayName = "ReadLine handles null return value from stream")]
    public async Task Should_Handle_Null_Return_Value()
    {
        // Arrange
        var readLine = new ReadLine();

        // Act
        var context = await ExecuteAsync(readLine, null);

        // Assert - The null-forgiving operator in ReadLine implementation means it expects non-null,
        // but null can occur at end of stream. Verify the behavior stores null.
        var result = context.GetActivityOutput(() => readLine.Result);
        Assert.Null(result);
    }

    [Fact(DisplayName = "ReadLine uses default provider when none is registered")]
    public async Task Should_Use_Default_Provider_When_None_Registered()
    {
        // Arrange
        var readLine = new ReadLine();
        const string expectedInput = "Default provider test";
        var originalConsoleIn = System.Console.In;

        try
        {
            // Redirect Console.In to our StringReader
            System.Console.SetIn(new StringReader(expectedInput));

            // Act
            var context = await new ActivityTestFixture(readLine).ExecuteAsync();

            // Assert
            var result = (string)context.GetActivityOutput(() => readLine.Result)!;
            Assert.Equal(expectedInput, result);
        }
        finally
        {
            // Restore original Console.In
            System.Console.SetIn(originalConsoleIn);
        }
    }

    private Task<ActivityExecutionContext> ExecuteAsync(ReadLine readLine, string? inputText = "Test")
    {
        _mockTextReader.ReadLine().Returns(inputText);
        return new ActivityTestFixture(readLine)
            .ConfigureServices(services => services.AddSingleton(_mockProvider))
            .ExecuteAsync();
    }
}

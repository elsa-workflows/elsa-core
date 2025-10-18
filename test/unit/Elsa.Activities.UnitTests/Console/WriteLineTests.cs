using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Console;

public class WriteLineTests
{
    private readonly TextWriter _mockTextWriter;
    private readonly IStandardOutStreamProvider _mockProvider;

    public WriteLineTests()
    {
        _mockTextWriter = Substitute.For<TextWriter>();
        _mockProvider = Substitute.For<IStandardOutStreamProvider>();
        _mockProvider.GetTextWriter().Returns(_mockTextWriter);
    }
    
    [Theory]
    [InlineData("Hello, World!")]
    [InlineData("")]
    public async Task Should_Write_Text_To_Output(string expectedText)
    {
        // Arrange
        var writeLine = new WriteLine(expectedText);

        // Act
        await ExecuteWriteLineAsync(writeLine);

        // Assert
        await _mockTextWriter.Received(1).WriteLineAsync(expectedText);
    }

    [Fact]
    public async Task Should_Write_Null_Value_To_Output()
    {
        // Arrange
        var writeLine = new WriteLine(new Input<string>(default(string)!));

        // Act
        await ExecuteWriteLineAsync(writeLine);

        // Assert
        await _mockTextWriter.Received(1).WriteLineAsync(Arg.Is<string?>(s => s == null));
    }

    [Fact]
    public async Task Should_Use_Default_Provider_When_None_Configured()
    {
        // Arrange
        const string textToWrite = "Default provider test";
        var writeLine = new WriteLine(textToWrite);

        // Act & Assert - Should not throw exception when no provider is configured
        var exception = await Record.ExceptionAsync(() => new ActivityTestFixture(writeLine).ExecuteAsync());

        Assert.Null(exception);
    }

    private async Task ExecuteWriteLineAsync(WriteLine writeLine)
    {
        await new ActivityTestFixture(writeLine)
            .ConfigureServices(services => services.AddSingleton(_mockProvider))
            .ExecuteAsync();
    }
}

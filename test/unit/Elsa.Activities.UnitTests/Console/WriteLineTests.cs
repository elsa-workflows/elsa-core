using Elsa.Activities.UnitTests.Helpers;
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
    
    [Fact]
    public async Task Should_Write_String_Literal_To_Output()
    {
        // Arrange
        const string expectedText = "Hello, World!";
        var writeLine = new WriteLine(expectedText);

        // Act
        var fixture = new ActivityTestFixture(writeLine)
            .ConfigureServices(services =>
            {
                services.AddSingleton(_mockProvider);
            });
        await fixture.ExecuteAsync();

        // Assert
        _mockTextWriter.Received(1).WriteLine(expectedText);
    }

    [Fact]
    public async Task Should_Write_Null_Value_To_Output()
    {
        // Arrange
        var writeLine = new WriteLine(new Input<string>((string?)null));

        // Act
        var fixture = new ActivityTestFixture(writeLine)
            .ConfigureServices(services =>
            {
                services.AddSingleton(_mockProvider);
            });
        await fixture.ExecuteAsync();

        // Assert
        _mockTextWriter.Received(1).WriteLine(Arg.Is<string?>(s => s == null));
    }

    [Fact]
    public async Task Should_Write_Empty_String_To_Output()
    {
        // Arrange
        const string expectedText = "";
        var writeLine = new WriteLine(expectedText);

        // Act
        var fixture = new ActivityTestFixture(writeLine)
            .ConfigureServices(services =>
            {
                services.AddSingleton(_mockProvider);
            });
        await fixture.ExecuteAsync();

        // Assert
        _mockTextWriter.Received(1).WriteLine(expectedText);
    }

    [Fact]
    public async Task Should_Use_Default_Provider_When_None_Configured()
    {
        // Arrange
        const string textToWrite = "Default provider test";
        var writeLine = new WriteLine(textToWrite);

        // Act & Assert - Should not throw exception when no provider is configured
        var exception = await Record.ExceptionAsync(async () =>
        {
            var fixture = new ActivityTestFixture(writeLine);
            await fixture.ExecuteAsync();
        });

        Assert.Null(exception);
    }
}

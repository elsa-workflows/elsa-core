using Elsa.Activities.UnitTests.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Console;

public class WriteLineTests
{
    [Fact]
    public async Task Should_Write_String_Literal_To_Output()
    {
        // Arrange
        const string expectedText = "Hello, World!";
        var mockTextWriter = Substitute.For<TextWriter>();
        var mockProvider = Substitute.For<IStandardOutStreamProvider>();
        mockProvider.GetTextWriter().Returns(mockTextWriter);
        
        var writeLine = new WriteLine(expectedText);

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(writeLine, services =>
        {
            services.AddSingleton(mockProvider);
        });

        // Assert
        mockTextWriter.Received(1).WriteLine(expectedText);
    }

    [Fact]
    public async Task Should_Write_Null_Value_To_Output()
    {
        // Arrange
        var mockTextWriter = Substitute.For<TextWriter>();
        var mockProvider = Substitute.For<IStandardOutStreamProvider>();
        mockProvider.GetTextWriter().Returns(mockTextWriter);
        
        var writeLine = new WriteLine(new Input<string>((string?)null));

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(writeLine, services =>
        {
            services.AddSingleton(mockProvider);
        });

        // Assert
        mockTextWriter.Received(1).WriteLine(Arg.Is<string?>(s => s == null));
    }

    [Fact]
    public async Task Should_Write_Empty_String_To_Output()
    {
        // Arrange
        const string expectedText = "";
        var mockTextWriter = Substitute.For<TextWriter>();
        var mockProvider = Substitute.For<IStandardOutStreamProvider>();
        mockProvider.GetTextWriter().Returns(mockTextWriter);
        
        var writeLine = new WriteLine(expectedText);

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(writeLine, services =>
        {
            services.AddSingleton(mockProvider);
        });

        // Assert
        mockTextWriter.Received(1).WriteLine(expectedText);
    }

    [Fact]
    public async Task Should_Use_Default_Provider_When_None_Configured()
    {
        // Arrange
        const string textToWrite = "Default provider test";
        var writeLine = new WriteLine(textToWrite);

        // Act & Assert - Should not throw exception when no provider is configured
        var exception = await Record.ExceptionAsync(async () => 
            await ActivityTestHelper.ExecuteActivityAsync(writeLine));
        
        Assert.Null(exception);
    }
}

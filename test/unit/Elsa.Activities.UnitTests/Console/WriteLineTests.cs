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
    public async Task Should_Write_Literal_Expression_To_Output()
    {
        // Arrange
        const string expectedText = "Literal expression";
        var literal = new Literal<string>(expectedText);
        var mockTextWriter = Substitute.For<TextWriter>();
        var mockProvider = Substitute.For<IStandardOutStreamProvider>();
        mockProvider.GetTextWriter().Returns(mockTextWriter);
        
        var writeLine = new WriteLine(literal);

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(writeLine, services =>
        {
            services.AddSingleton(mockProvider);
        });

        // Assert
        mockTextWriter.Received(1).WriteLine(expectedText);
    }

    [Fact]
    public async Task Should_Write_Delegate_Function_Result_To_Output()
    {
        // Arrange
        const string expectedText = "Function result";
        Func<string> textFunc = () => expectedText;
        var mockTextWriter = Substitute.For<TextWriter>();
        var mockProvider = Substitute.For<IStandardOutStreamProvider>();
        mockProvider.GetTextWriter().Returns(mockTextWriter);
        
        var writeLine = new WriteLine(textFunc);

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(writeLine, services =>
        {
            services.AddSingleton(mockProvider);
        });

        // Assert
        mockTextWriter.Received(1).WriteLine(expectedText);
    }

    [Fact]
    public async Task Should_Write_Expression_Context_Function_Result_To_Output()
    {
        // Arrange
        const string expectedText = "Context function result";
        Func<ExpressionExecutionContext, string?> textFunc = _ => expectedText;
        var mockTextWriter = Substitute.For<TextWriter>();
        var mockProvider = Substitute.For<IStandardOutStreamProvider>();
        mockProvider.GetTextWriter().Returns(mockTextWriter);
        
        var writeLine = new WriteLine(textFunc);

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(writeLine, services =>
        {
            services.AddSingleton(mockProvider);
        });

        // Assert
        mockTextWriter.Received(1).WriteLine(expectedText);
    }

    [Fact]
    public async Task Should_Write_Input_Value_To_Output()
    {
        // Arrange
        const string expectedText = "Input value";
        var input = new Input<string>(expectedText);
        var mockTextWriter = Substitute.For<TextWriter>();
        var mockProvider = Substitute.For<IStandardOutStreamProvider>();
        mockProvider.GetTextWriter().Returns(mockTextWriter);
        
        var writeLine = new WriteLine(input);

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
        
        var writeLine = new WriteLine(new Input<string>(default(string)!));

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
        const string expectedText = "Default provider test";
        var writeLine = new WriteLine(expectedText);

        // Act & Assert - Should not throw exception when no provider is configured
        var exception = await Record.ExceptionAsync(async () => 
            await ActivityTestHelper.ExecuteActivityAsync(writeLine));
        
        Assert.Null(exception);
    }
}

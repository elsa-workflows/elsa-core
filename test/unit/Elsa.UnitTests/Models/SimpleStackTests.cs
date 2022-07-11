using Xunit;

namespace Elsa.Models;

public class SimpleStackTests
{
    [Fact(DisplayName = "Testing simple stack push, pop, peek, and remove operations")]
    public void PushPopAndRemove()
    {
        var sut = new SimpleStack<string>();
        
        sut.Push("1");
        sut.Push("2");
        sut.Push("3");
        sut.Push("4");
        
        Assert.Equal("4", sut.Peek());
        Assert.Equal("4", sut.Peek());
        Assert.Equal("4", sut.Pop());
        Assert.Equal("3", sut.Peek());

        sut.Remove("2");
        
        Assert.Equal("3", sut.Pop());
        Assert.Equal("1", sut.Pop());
    }
}
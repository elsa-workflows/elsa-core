using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Elsa.Agents.Plugins;

[Description("Contains arithmetic operations")]
public class Arithmetics
{
    [KernelFunction("sum")]
    [Description("Adds a number to the given number")]
    [return: Description("The sum of the given numbers")]
    public double Sum(double a, double b)
    {
        return a + b;
    }

    
}
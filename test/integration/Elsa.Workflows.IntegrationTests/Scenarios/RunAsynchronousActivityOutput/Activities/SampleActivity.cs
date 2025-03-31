﻿using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.RunAsynchronousActivityOutput.Activities;

[Activity("Elsa.Test", Kind = ActivityKind.Task)]
internal class SampleActivity : CodeActivity
{

    public SampleActivity()
    {
        RunAsynchronously = true;
    }

    public Input<int>? Number1 { get; set; } = default;
    public Input<int>? Number2 { get; set; } = default;
    public Output<int>? Sum { get; set; } = default;
    public Output<int>? Product { get; set; } = default;

    protected override void Execute(ActivityExecutionContext context)
    {
        int number1 = context.Get(Number1);
        int number2 = context.Get(Number2);

        context.Set(Sum, number1 + number2);
        context.Set(Product, number1 * number2);
    }
}

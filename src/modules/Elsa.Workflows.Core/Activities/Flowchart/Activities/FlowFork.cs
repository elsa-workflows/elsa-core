﻿using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// Branch execution into multiple branches that will be executed in parallel.
/// </summary>
[Activity("Elsa", "Branching", "Branch execution into multiple branches that will be executed in parallel.", DisplayName = "Fork (flow)")]
[PublicAPI]
public class FlowFork : Activity
{
    /// <inheritdoc />
    public FlowFork([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// A list of expected outcomes to handle.
    /// </summary>
    [Input(
        Description = "A list of expected outcomes to handle.",
        UIHint = InputUIHints.DynamicOutcomes
    )]
    public Input<ICollection<string>> Branches { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var outcomes = Branches.GetOrDefault(context)?.ToArray() ?? new[] { "Done" };

        await context.ScheduleOutcomesAsync(outcomes);
    }
}

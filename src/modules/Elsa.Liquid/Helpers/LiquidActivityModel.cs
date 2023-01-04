﻿using Elsa.Workflows.Core.Models;

namespace Elsa.Liquid.Helpers;

public record LiquidActivityModel(ActivityExecutionContext ActivityExecutionContext, string? ActivityName, string? ActivityId)
{
}
using System;
using Elsa.Workflows.Runtime.Implementations;

namespace Elsa.Workflows.Runtime.Attributes;

/// <summary>
/// Used by <see cref="TriggerIndexer"/> to skip workflows from providers annotated with this attributed.  
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SkipTriggerIndexingAttribute : Attribute
{
}
using System;
using Elsa.Services.Triggers;

namespace Elsa.Attributes;

/// <summary>
/// Used by <see cref="TriggerIndexer"/> to skip workflows from providers annotated with this attributed during startup.  
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SkipTriggerIndexingAttribute : Attribute
{
}
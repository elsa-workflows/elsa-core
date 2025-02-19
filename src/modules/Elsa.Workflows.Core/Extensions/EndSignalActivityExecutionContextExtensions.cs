namespace Elsa.Workflows.Extensions;

public static class EndSignalActivityExecutionContextExtensions
{
  /// <summary>
  /// Gets a value indicating whether the current activity is ending out of a flowchart.
  /// </summary>
  public static bool GetIsEnding(this ActivityExecutionContext context) => context.GetProperty<bool>("IsEnding");

  /// <summary>
  /// Sets a value indicating whether the current activity is ending out of a flowchart.
  /// <summary>
  public static void SetIsEnding(this ActivityExecutionContext context) => context.SetProperty("IsEnding", true);
}

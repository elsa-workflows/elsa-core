using Elsa.Studio.Models;

namespace Elsa.Studio.Authentication.UI.Models;

/// <summary>
/// Stable identifiers for the themes supplied by Elsa Studio packages.
/// </summary>
public static class LoginThemeIds
{
    public const string Inherit = "inherit";
    public const string Classic = StudioThemeIds.Classic;
    public const string ClassicUnified = "classic-unified";
    public const string ClassicRefinedSplit = "classic-refined-split";
    public const string ClassicBrandCanvas = "classic-brand-canvas";
    public const string WorkflowConstellation = StudioThemeIds.WorkflowConstellation;
    public const string WorkflowAurora = StudioThemeIds.WorkflowAurora;
    public const string ExecutionTimeline = StudioThemeIds.ExecutionTimeline;
    public const string HumanAutomation = StudioThemeIds.HumanAutomation;
}

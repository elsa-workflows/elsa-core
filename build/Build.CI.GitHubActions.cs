using Nuke.Common.CI.GitHubActions;
using Nuke.Components;

[GitHubActions(
        "pr",
        GitHubActionsImage.UbuntuLatest,
        On = [GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest, GitHubActionsTrigger.WorkflowDispatch],
        OnPushBranches = ["main", "feature/*", "patch/*", "fix/*", "enhancement/*"],
        OnPullRequestBranches = ["main", "feature/*", "patch/*", "fix/*", "enhancement/*"],
        OnPullRequestIncludePaths = ["**/*"],
        PublishArtifacts = false,
        InvokedTargets = [nameof(ICompile.Compile), nameof(ITest.Test), nameof(IPack.Pack)],
        CacheKeyFiles = []
    )
]
public partial class Build;
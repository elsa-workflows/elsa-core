using Nuke.Common.CI.GitHubActions;
using Nuke.Components;

[GitHubActions(
        "pr",
        GitHubActionsImage.UbuntuLatest,
        OnPullRequestBranches = ["main"],
        OnPullRequestIncludePaths = ["**/*"],
        PublishArtifacts = false,
        InvokedTargets = [nameof(ICompile.Compile), nameof(ITest.Test), nameof(IPack.Pack)],
        CacheKeyFiles = [],
        ConcurrencyCancelInProgress = true
    )
]
public partial class Build;
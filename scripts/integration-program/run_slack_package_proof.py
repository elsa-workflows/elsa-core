#!/usr/bin/env python3
"""Build and verify the Elsa.Slack 3.8.4-source local package proof.

Proof artifacts and package caches live under a new output directory. Dotnet
restore/build also writes ignored bin/obj files under the disposable source
worktrees. Nothing is published.
"""

from __future__ import annotations

import argparse
import hashlib
import html
import json
import os
import shutil
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path
from xml.etree import ElementTree

CORE_SHA = "33181ae3048f628f591a0155b5665a8e4d1bcea2"
EXTENSIONS_SHA = "154ba15fb4da85b4bebecfbe43639579cbda1d0d"
OFFICIAL_SHA256 = "6df6fd1c3e7558c7c1124fa232879cfa539196fa35a3edd9055cfcc2e0a178e6"
TFMS = ("net8.0", "net9.0", "net10.0")
PACKAGE_ID = "Elsa.Slack"
PROOF_VERSION = "3.8.5-proof.154ba15"
PROJECT_RELATIVE = Path("src/modules/communication/Elsa.Slack/Elsa.Slack.csproj")
TEST_RELATIVE = Path("test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj")
INVENTORY_RELATIVE = Path("doc/integration-program/inventory/inventory.json")


def run(command: list[str], *, cwd: Path, env: dict[str, str], log: Path) -> str:
    log.parent.mkdir(parents=True, exist_ok=True)
    rendered = subprocess.list2cmdline(command)
    with log.open("w", encoding="utf-8") as output:
        output.write(f"$ {rendered}\n")
        output.flush()
        result = subprocess.run(
            command,
            cwd=cwd,
            env=env,
            text=True,
            stdout=output,
            stderr=subprocess.STDOUT,
            check=False,
        )
    if result.returncode:
        raise RuntimeError(f"Command failed ({result.returncode}); see {log}: {rendered}")
    return rendered


def git_value(root: Path, *args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=root, text=True).strip()


def require_clean_pin(root: Path, expected_sha: str, name: str) -> None:
    actual_sha = git_value(root, "rev-parse", "HEAD")
    status = git_value(root, "status", "--porcelain")
    if actual_sha != expected_sha or status:
        raise RuntimeError(
            f"{name} source must be clean at {expected_sha}; found {actual_sha}, status={status!r}"
        )


def require_core_project_reference(project: Path, expected_core_project: Path) -> None:
    project_root = ElementTree.parse(project).getroot()
    includes = [
        item.get("Include")
        for item in project_root.findall(".//{*}ProjectReference")
        if item.get("Include")
    ]
    resolved = [
        (project.parent / Path(include.replace("\\", os.sep))).resolve()
        for include in includes
    ]
    expected = expected_core_project.resolve()
    if resolved != [expected]:
        raise RuntimeError(
            f"Slack project references {resolved}; expected exactly the supplied Core project {expected}"
        )


def require_sourcelink_tool(tool_path: Path) -> tuple[Path, str]:
    tool = tool_path.resolve()
    if tool.stem.lower() != "sourcelink" or not tool.is_file() or not os.access(tool, os.X_OK):
        raise RuntimeError(f"SourceLink CLI must be an executable sourcelink 3.1.1 tool: {tool}")

    result = subprocess.run(
        ["dotnet", "tool", "list", "--tool-path", str(tool.parent)],
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        raise RuntimeError(f"Could not verify the pinned SourceLink CLI version: {result.stderr.strip()}")

    matching = [
        fields
        for line in result.stdout.splitlines()
        if len(fields := line.split()) >= 3 and fields[0].lower() == "sourcelink"
    ]
    if len(matching) != 1 or matching[0][1] != "3.1.1" or matching[0][2].lower() != "sourcelink":
        raise RuntimeError(
            f"Expected SourceLink CLI 3.1.1 at {tool}; dotnet tool list returned {matching}"
        )
    return tool, matching[0][1]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def write_nuget_config(
    path: Path,
    package_sources: list[tuple[str, str]],
    source_mapping: dict[str, list[str]] | None = None,
) -> None:
    sources = "\n".join(
        f'    <add key="{html.escape(key, quote=True)}" value="{html.escape(value, quote=True)}" />'
        for key, value in package_sources
    )
    mapping = ""
    if source_mapping:
        mapping_sources = []
        for source, patterns in source_mapping.items():
            rendered_patterns = "".join(
                f'<package pattern="{html.escape(pattern, quote=True)}" />' for pattern in patterns
            )
            mapping_sources.append(
                f'<packageSource key="{html.escape(source, quote=True)}">{rendered_patterns}</packageSource>'
            )
        mapping = "<packageSourceMapping>" + "".join(mapping_sources) + "</packageSourceMapping>"
    path.write_text(
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        "<configuration><packageSources><clear />\n"
        f"{sources}\n"
        "</packageSources>"
        f"{mapping}</configuration>\n",
        encoding="utf-8",
    )


def official_package(destination: Path) -> dict[str, str]:
    url = "https://api.nuget.org/v3-flatcontainer/elsa.slack/3.8.4/elsa.slack.3.8.4.nupkg"
    request = urllib.request.Request(url, headers={"User-Agent": "Elsa-local-package-proof/1.0"})
    with urllib.request.urlopen(request, timeout=60) as response, destination.open("wb") as output:
        shutil.copyfileobj(response, output)
    digest = sha256(destination)
    if digest != OFFICIAL_SHA256:
        raise RuntimeError(f"Public Elsa.Slack 3.8.4 hash changed: {digest}")
    return {"url": url, "version": "3.8.4", "sha256": digest}


def inspect_package(path: Path, expected_version: str, expected_commit: str) -> dict:
    with zipfile.ZipFile(path) as archive:
        nuspec_names = [name for name in archive.namelist() if name.lower().endswith(".nuspec")]
        if len(nuspec_names) != 1:
            raise RuntimeError(f"Expected one nuspec in {path}, found {nuspec_names}")
        root = ElementTree.fromstring(archive.read(nuspec_names[0]))

    metadata = root.find("{*}metadata")
    if metadata is None:
        raise RuntimeError(f"Missing nuspec metadata in {path}")
    package_id = metadata.findtext("{*}id")
    version = metadata.findtext("{*}version")
    if package_id != PACKAGE_ID or version != expected_version:
        raise RuntimeError(f"Unexpected package identity {package_id} {version} in {path}")

    repository = metadata.find("{*}repository")
    repository_url = repository.get("url") if repository is not None else None
    repository_commit = repository.get("commit") if repository is not None else None
    if repository_commit != expected_commit:
        raise RuntimeError(f"Package repository commit {repository_commit!r}, expected {expected_commit}")

    groups = metadata.findall("{*}dependencies/{*}group")
    tfms = sorted(group.get("targetFramework", "") for group in groups)
    dependencies = {
        group.get("targetFramework", ""): sorted(
            (dependency.get("id"), dependency.get("version"))
            for dependency in group.findall("{*}dependency")
        )
        for group in groups
    }
    if set(tfms) != {"net8.0", "net9.0", "net10.0"}:
        raise RuntimeError(f"Unexpected target framework groups: {tfms}")
    for target, packages in dependencies.items():
        if packages != [("Elsa", "3.8.4"), ("SlackNet", "0.17.7")]:
            raise RuntimeError(f"Unexpected dependencies for {target}: {packages}")

    return {
        "package_id": package_id,
        "version": version,
        "repository_url": repository_url,
        "repository_commit": repository_commit,
        "target_frameworks": tfms,
        "dependencies": dependencies,
        "sha256": sha256(path),
    }


def consumer_project(root: Path, framework: str, *, package_version: str | None = None, project_reference: Path | None = None) -> Path:
    project = root / "PackageSmoke.csproj"
    if bool(package_version) == bool(project_reference):
        raise ValueError("Choose exactly one package version or project reference")
    package_item = (
        f'<PackageReference Include="{PACKAGE_ID}" Version="{package_version}" />'
        if package_version
        else f'<ProjectReference Include="{project_reference}" />'
    )
    project.write_text(
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
        "  <PropertyGroup>\n"
        f"    <TargetFramework>{framework}</TargetFramework>\n"
        "    <OutputType>Exe</OutputType>\n"
        "    <ImplicitUsings>enable</ImplicitUsings>\n"
        "    <Nullable>enable</Nullable>\n"
        "  </PropertyGroup>\n"
        "  <ItemGroup>\n"
        f"    {package_item}\n"
        "  </ItemGroup>\n"
        "</Project>\n",
        encoding="utf-8",
    )
    (root / "Program.cs").write_text(
        "using Elsa.Extensions;\n"
        "using Elsa.Slack.Activities.Channels;\n"
        "using Elsa.Workflows;\n"
        "using Elsa.Workflows.Helpers;\n"
        "using Microsoft.Extensions.DependencyInjection;\n"
        "using System.Text.Json;\n"
        "\n"
        "var services = new ServiceCollection();\n"
        "services.AddElsa(elsa => elsa.AddActivity<CreateChannel>());\n"
        "await using var serviceProvider = services.BuildServiceProvider();\n"
        "var registry = serviceProvider.GetRequiredService<IActivityRegistry>();\n"
        "await registry.RegisterAsync(typeof(CreateChannel));\n"
        "var typeName = ActivityTypeNameHelper.GenerateTypeName(typeof(CreateChannel));\n"
        "var descriptor = registry.Find(typeName) ?? throw new InvalidOperationException($\"Missing activity descriptor: {typeName}\");\n"
        "Console.WriteLine(\"ELSA_ACTIVITY_DESCRIPTOR=\" + JsonSerializer.Serialize(new { descriptor.TypeName, descriptor.Version }));\n",
        encoding="utf-8",
    )
    return project


def offline_activity_smoke_project(root: Path, package_version: str) -> Path:
    project = root / "OfflineActivitySmoke.csproj"
    project.write_text(
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
        "  <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType>"
        "<ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>\n"
        "  <ItemGroup>\n"
        f"    <PackageReference Include=\"{PACKAGE_ID}\" Version=\"{package_version}\" />\n"
        "    <PackageReference Include=\"Elsa.Testing.Shared.Integration\" Version=\"3.8.4\" />\n"
        "  </ItemGroup>\n"
        "</Project>\n",
        encoding="utf-8",
    )
    (root / "Program.cs").write_text(
        """using System.Reflection;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Slack.Activities.Channels;
using Elsa.Slack.Services;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using SlackNet;
using SlackNet.WebApi;
using Xunit.Abstractions;

const string token = "offline-proof-token-never-sent";
const string requestedName = "offline-proof-room";
const string requestedTeamId = "T_OFFLINE_PROOF";
const string expectedChannelId = "C_OFFLINE_PROOF";

var fixture = new WorkflowTestFixture(new NoopTestOutput());
fixture.ConfigureElsa(elsa => elsa.AddActivity<CreateChannel>());
fixture.ConfigureServices(services => services.AddSingleton<SlackClientFactory>());
await fixture.BuildAsync();

var slackFactory = fixture.Services.GetRequiredService<SlackClientFactory>();
var cacheField = typeof(SlackClientFactory).GetField("_slackClients", BindingFlags.Instance | BindingFlags.NonPublic)
    ?? throw new InvalidOperationException("SlackClientFactory cache field changed; refusing to execute the activity.");
if (cacheField.FieldType != typeof(Dictionary<string, ISlackApiClient>))
    throw new InvalidOperationException($"Unexpected SlackClientFactory cache type {cacheField.FieldType}; refusing to execute the activity.");
var cache = (Dictionary<string, ISlackApiClient>)(cacheField.GetValue(slackFactory)
    ?? throw new InvalidOperationException("SlackClientFactory cache is null; refusing to execute the activity."));

var fakeConversations = DispatchProxy.Create<IConversationsApi, FakeConversationsApi>();
var fakeClient = DispatchProxy.Create<ISlackApiClient, FakeSlackApiClient>();
((FakeSlackApiClient)(object)fakeClient).Conversations = fakeConversations;
if (cache.ContainsKey(token))
    throw new InvalidOperationException("Offline proof token unexpectedly exists in the client cache.");
cache.Add(token, fakeClient);
if (!ReferenceEquals(slackFactory.GetClient(token), fakeClient))
    throw new InvalidOperationException("SlackClientFactory did not resolve the seeded fake; refusing to execute the activity.");

var activity = new CreateChannel
{
    Token = new Input<string>(token),
    ChannelName = new Input<string>(requestedName),
    IsPrivate = new Input<bool>(true),
    TeamId = new Input<string>(requestedTeamId)
};
var result = await fixture.RunActivityAsync(activity);
var context = result.Journal.ActivityExecutionContexts.Single(x => x.Activity.Id == activity.Id);
var channel = context.GetActivityOutput(() => activity.Channel) as Conversation;
var conversationsApi = (FakeConversationsApi)(object)fakeConversations;
if (conversationsApi.CreateCalls != 1 || conversationsApi.ChannelName != requestedName ||
    conversationsApi.IsPrivate != true || conversationsApi.TeamId != requestedTeamId)
    throw new InvalidOperationException("CreateChannel did not forward the expected request to the fake Slack client.");
if (channel?.Id != expectedChannelId || channel.Name != requestedName)
    throw new InvalidOperationException("CreateChannel did not write the fake channel response to its output.");

Console.WriteLine("ELSA_OFFLINE_ACTIVITY_SMOKE=" + JsonSerializer.Serialize(new
{
    activity = "CreateChannel",
    request = new { conversationsApi.ChannelName, conversationsApi.IsPrivate, conversationsApi.TeamId },
    output = new { channel.Id, channel.Name },
    fakeCalls = conversationsApi.CreateCalls
}));

public class FakeSlackApiClient : DispatchProxy
{
    public IConversationsApi Conversations { get; set; } = null!;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        targetMethod?.Name == "get_Conversations"
            ? Conversations
            : throw new InvalidOperationException($"Unexpected fake Slack client call: {targetMethod?.Name}");
}

public class FakeConversationsApi : DispatchProxy
{
    public int CreateCalls { get; private set; }
    public string? ChannelName { get; private set; }
    public bool? IsPrivate { get; private set; }
    public string? TeamId { get; private set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name != nameof(IConversationsApi.Create) || args is null || args.Length < 3)
            throw new InvalidOperationException($"Unexpected fake Conversations API call: {targetMethod?.Name}");
        CreateCalls++;
        ChannelName = (string)args[0]!;
        IsPrivate = (bool)args[1]!;
        TeamId = (string?)args[2];
        return Task.FromResult(new Conversation { Id = "C_OFFLINE_PROOF", Name = ChannelName });
    }
}

public class NoopTestOutput : ITestOutputHelper
{
    public void WriteLine(string message) { }
    public void WriteLine(string format, params object[] args) { }
}
""",
        encoding="utf-8",
    )
    return project


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--core-source", type=Path, required=True)
    parser.add_argument("--extensions-source", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--sourcelink-tool", type=Path, required=True)
    args = parser.parse_args()

    source_link_tool, source_link_version = require_sourcelink_tool(args.sourcelink_tool)
    core_source = args.core_source.resolve()
    extensions_source = args.extensions_source.resolve()
    output = args.output_dir.resolve()
    if output.exists() and any(output.iterdir()):
        raise RuntimeError(f"Output directory must be absent or empty: {output}")
    output.mkdir(parents=True, exist_ok=True)

    require_clean_pin(core_source, CORE_SHA, "Elsa Core")
    require_clean_pin(extensions_source, EXTENSIONS_SHA, "Elsa Extensions")
    project = extensions_source / PROJECT_RELATIVE
    test_project = extensions_source / TEST_RELATIVE
    if not project.is_file() or not test_project.is_file():
        raise RuntimeError("Pinned Slack project or focused test project is missing")
    require_core_project_reference(project, core_source / "src/modules/Elsa/Elsa.csproj")

    packages = output / "local-feed"
    packages.mkdir()
    cache_root = output / "package-caches"
    cache_root.mkdir()
    nuget_config = output / "NuGet.Config"
    write_nuget_config(nuget_config, [("nuget.org", "https://api.nuget.org/v3/index.json")])
    source_nuget_config = output / "Source-NuGet.Config"
    source_feeds = [
        ("nuget.org", "https://api.nuget.org/v3/index.json"),
        ("elsa-feedz", "https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json"),
    ]
    source_mapping = {
        "nuget.org": ["*"],
        "elsa-feedz": ["Elsa.Platform.PackageManifest.Generator"],
    }
    write_nuget_config(source_nuget_config, source_feeds, source_mapping)
    env = os.environ.copy()
    env["DOTNET_CLI_HOME"] = str(output / ".dotnet-home")
    env["NUGET_HTTP_CACHE_PATH"] = str(cache_root / "http")

    evidence: dict = {
        "scope": "local-only Elsa.Slack package proof; no feed publication",
        "source": {
            "elsa_core": {"path": str(core_source), "commit": CORE_SHA},
            "elsa_extensions": {"path": str(extensions_source), "commit": EXTENSIONS_SHA},
        },
        "source_project_reference_guard": {
            "result": "passed",
            "extensions_project": str(project),
            "core_project": str(core_source / "src/modules/Elsa/Elsa.csproj"),
        },
        "frameworks": list(TFMS),
        "package_version": PROOF_VERSION,
        "source_link_tool": {"path": str(source_link_tool), "version": source_link_version},
        "source_build_tooling_feed": "Core source restore resolves Elsa.Platform.PackageManifest.Generator 0.0.1-preview.53 from Elsa Feedz; package consumers remain NuGet.org-only",
        "commands": [],
    }

    inventory = Path(__file__).resolve().parents[2] / INVENTORY_RELATIVE
    impact = subprocess.run(
        [
            sys.executable,
            str(Path(__file__).with_name("package_impact.py")),
            "--inventory",
            str(inventory),
            "--changed",
            "elsa-core:src/modules/Elsa/Elsa.csproj",
            "--release-unit",
            "elsa-extensions:src/modules/communication/Elsa.Slack/Elsa.Slack.csproj",
        ],
        check=True,
        text=True,
        capture_output=True,
    )
    impact_json = json.loads(impact.stdout)
    if impact_json["package_ids_to_pack"] != [PACKAGE_ID]:
        raise RuntimeError(f"Slack-only selection changed unexpectedly: {impact_json}")
    if not any(item.endswith(TEST_RELATIVE.as_posix()) for item in impact_json["affected_test_projects"]):
        raise RuntimeError("Core Elsa project change did not select the Slack test project")
    impact_path = output / "impact-selection.json"
    impact_path.write_text(json.dumps(impact_json, indent=2) + "\n", encoding="utf-8")
    evidence["impact_selection"] = impact_json

    source_build_cache = cache_root / "source-build"
    source_test_cache = cache_root / "source-test"
    env["NUGET_PACKAGES"] = str(source_build_cache)
    evidence["commands"].append(
        run(
            [
                "dotnet", "restore", str(project), "--configfile", str(source_nuget_config),
                "-p:UseProjectReferences=true", "-p:TargetFrameworks=net10.0",
            ],
            cwd=extensions_source,
            env=env,
            log=output / "logs/source-debug-restore.log",
        )
    )
    evidence["commands"].append(
        run(
            [
                "dotnet", "build", str(project), "--no-restore", "--framework", "net10.0",
                "-p:UseProjectReferences=true", "-p:ContinuousIntegrationBuild=true",
            ],
            cwd=extensions_source,
            env=env,
            log=output / "logs/source-debug-build.log",
        )
    )

    env["NUGET_PACKAGES"] = str(source_test_cache)
    evidence["commands"].append(
        run(
            [
                "dotnet", "restore", str(test_project), "--configfile", str(nuget_config),
                "-p:TargetFramework=net10.0",
            ],
            cwd=extensions_source,
            env=env,
            log=output / "logs/slack-tests-restore.log",
        )
    )
    evidence["commands"].append(
        run(
            [
                "dotnet", "test", str(test_project), "--no-restore", "--framework", "net10.0",
                "--logger", "trx", "--results-directory", str(output / "test-results"),
            ],
            cwd=extensions_source,
            env=env,
            log=output / "logs/slack-tests.log",
        )
    )
    trx_files = sorted((output / "test-results").glob("*.trx"))
    if not trx_files:
        raise RuntimeError("Focused Slack test command produced no TRX result")
    trx_root = ElementTree.parse(trx_files[0]).getroot()
    trx_counters = trx_root.find("{*}ResultSummary/{*}Counters")
    if trx_counters is None:
        raise RuntimeError(f"Missing test counters in {trx_files[0]}")
    counts = {name: int(trx_counters.get(name, "0")) for name in ("total", "executed", "passed", "failed", "notExecuted")}
    test_results = trx_root.findall(".//{*}UnitTestResult")
    counts["skipped"] = sum(
        result.get("outcome") == "NotExecuted"
        and result.find("{*}Output/{*}ErrorInfo/{*}Message") is not None
        for result in test_results
    )
    if counts["failed"]:
        raise RuntimeError(f"Focused Slack test project reports failures: {counts}")
    evidence["existing_focused_tests"] = {
        "project": TEST_RELATIVE.as_posix(),
        "framework": "net10.0",
        "result": counts,
        "note": "The only currently defined Slack test is skipped as 'Not implemented yet'; no connector behavior test passed.",
    }

    official_path = output / "Elsa.Slack.3.8.4.public.nupkg"
    evidence["official_artifact"] = official_package(official_path)
    evidence["official_artifact"]["nuspec"] = inspect_package(
        official_path, "3.8.4", EXTENSIONS_SHA
    )

    env["NUGET_PACKAGES"] = str(cache_root / "local-pack")
    evidence["commands"].append(
        run(
            [
                "dotnet", "restore", str(project), "--configfile", str(nuget_config),
                "-p:UseProjectReferences=false", f"-p:PackageVersion={PROOF_VERSION}",
            ],
            cwd=extensions_source,
            env=env,
            log=output / "logs/pack-restore.log",
        )
    )
    evidence["commands"].append(
        run(
            [
                "dotnet", "pack", str(project), "--no-restore", "--output", str(packages),
                "-p:UseProjectReferences=false", f"-p:PackageVersion={PROOF_VERSION}",
                "-p:ContinuousIntegrationBuild=true", "-p:IncludeSymbols=true",
                "-p:SymbolPackageFormat=snupkg",
            ],
            cwd=extensions_source,
            env=env,
            log=output / "logs/pack.log",
        )
    )

    nupkgs = sorted(packages.glob("*.nupkg"))
    if [path.name for path in nupkgs] != [f"{PACKAGE_ID}.{PROOF_VERSION}.nupkg"]:
        raise RuntimeError(f"Local feed contains unrelated or missing packages: {[p.name for p in nupkgs]}")
    packed_path = nupkgs[0]
    evidence["local_artifact"] = inspect_package(packed_path, PROOF_VERSION, EXTENSIONS_SHA)

    symbol_packages = sorted(packages.glob("*.snupkg"))
    if len(symbol_packages) != 1:
        raise RuntimeError(f"Expected one symbols package, found {[p.name for p in symbol_packages]}")
    source_link_dir = output / "source-link"
    source_link_dir.mkdir()
    source_pdb = source_link_dir / "Elsa.Slack.net10.0.pdb"
    with zipfile.ZipFile(symbol_packages[0]) as symbols:
        source_pdb.write_bytes(symbols.read("lib/net10.0/Elsa.Slack.pdb"))
    evidence["commands"].append(
        run(
            [str(source_link_tool), "print-json", str(source_pdb)],
            cwd=extensions_source,
            env=env,
            log=output / "logs/sourcelink-print-json.log",
        )
    )
    sourcelink_output = output / "logs/sourcelink-print-json.log"
    parsed_source_link = json.loads("\n".join(sourcelink_output.read_text().splitlines()[1:]))
    document_urls = list(parsed_source_link.get("documents", {}).values())
    if not document_urls or not all(EXTENSIONS_SHA in url for url in document_urls):
        raise RuntimeError(f"SourceLink documents do not resolve to pinned commit {EXTENSIONS_SHA}: {parsed_source_link}")
    evidence["commands"].append(
        run(
            [str(source_link_tool), "test", str(source_pdb)],
            cwd=extensions_source,
            env=env,
            log=output / "logs/sourcelink-test.log",
        )
    )
    evidence["source_link_validation"] = {
        "result": f"passed with sourcelink {source_link_version}",
        "source_commit": EXTENSIONS_SHA,
        "documents": parsed_source_link["documents"],
    }

    consumer_results = []
    source_list = [
        ("nuget.org", "https://api.nuget.org/v3/index.json"),
        ("local-feed", str(packages)),
    ]
    for version, label in (("3.8.4", "public-3.8.4"), (PROOF_VERSION, "local-proof")):
        for framework in TFMS:
            consumer_dir = output / "consumers" / label / framework
            consumer_dir.mkdir(parents=True)
            project_path = consumer_project(consumer_dir, framework, package_version=version)
            config = consumer_dir / "NuGet.Config"
            write_nuget_config(config, source_list)
            env["NUGET_PACKAGES"] = str(cache_root / label / framework)
            command = [
                "dotnet", "run", "--project", str(project_path), "--framework", framework,
            ]
            evidence["commands"].append(
                run(
                    ["dotnet", "restore", str(project_path), "--configfile", str(config)],
                    cwd=consumer_dir,
                    env=env,
                    log=output / "logs" / f"consumer-{label}-{framework}-restore.log",
                )
            )
            command[2:2] = ["--no-restore"]
            evidence["commands"].append(
                run(
                    command,
                    cwd=consumer_dir,
                    env=env,
                    log=output / "logs" / f"consumer-{label}-{framework}.log",
                )
            )
            log_path = output / "logs" / f"consumer-{label}-{framework}.log"
            marker = next(
                (line.split("ELSA_ACTIVITY_DESCRIPTOR=", 1)[1] for line in log_path.read_text().splitlines() if "ELSA_ACTIVITY_DESCRIPTOR=" in line),
                None,
            )
            if not marker:
                raise RuntimeError(f"Consumer did not emit a runtime activity descriptor; see {log_path}")
            descriptor = json.loads(marker)
            if not descriptor.get("TypeName") or descriptor.get("Version", 0) < 1:
                raise RuntimeError(f"Invalid activity descriptor: {descriptor}")
            consumer_results.append({"artifact": label, "framework": framework, "result": "passed", "descriptor": descriptor})
    evidence["consumer_smoke"] = consumer_results
    expected_descriptor = consumer_results[0]["descriptor"]
    if any(item["descriptor"] != expected_descriptor for item in consumer_results):
        raise RuntimeError("Released artifact descriptors differ across package versions or target frameworks")

    offline_smoke_results = []
    for version, label in (("3.8.4", "public-3.8.4"), (PROOF_VERSION, "local-proof")):
        smoke_dir = output / "consumers" / "offline-activity-smoke" / label
        smoke_dir.mkdir(parents=True)
        smoke_project = offline_activity_smoke_project(smoke_dir, version)
        smoke_config = smoke_dir / "NuGet.Config"
        write_nuget_config(smoke_config, source_list)
        cache_path = cache_root / "offline-activity-smoke" / label
        env["NUGET_PACKAGES"] = str(cache_path)
        evidence["commands"].append(
            run(
                ["dotnet", "restore", str(smoke_project), "--configfile", str(smoke_config)],
                cwd=smoke_dir,
                env=env,
                log=output / "logs" / f"offline-activity-smoke-{label}-restore.log",
            )
        )
        # The proof token is injected into the exact client-factory cache entry. A
        # cache preflight fails before activity execution if the fake is not used.
        offline_env = env.copy()
        offline_env["HTTP_PROXY"] = "http://127.0.0.1:9"
        offline_env["HTTPS_PROXY"] = "http://127.0.0.1:9"
        offline_env["ALL_PROXY"] = "http://127.0.0.1:9"
        offline_env["NO_PROXY"] = "localhost,127.0.0.1,::1"
        offline_log = output / "logs" / f"offline-activity-smoke-{label}.log"
        evidence["commands"].append(
            run(
                ["dotnet", "run", "--no-restore", "--project", str(smoke_project), "--framework", "net10.0"],
                cwd=smoke_dir,
                env=offline_env,
                log=offline_log,
            )
        )
        marker = next(
            (line.split("ELSA_OFFLINE_ACTIVITY_SMOKE=", 1)[1] for line in offline_log.read_text().splitlines() if "ELSA_OFFLINE_ACTIVITY_SMOKE=" in line),
            None,
        )
        if not marker:
            raise RuntimeError(f"Offline CreateChannel consumer emitted no result; see {offline_log}")
        result = json.loads(marker)
        if result.get("fakeCalls") != 1 or result.get("output", {}).get("Id") != "C_OFFLINE_PROOF":
            raise RuntimeError(f"Offline CreateChannel contract failed: {result}")
        offline_smoke_results.append({"artifact": label, "framework": "net10.0", "result": "passed", "receipt": result})
    if offline_smoke_results[0]["receipt"] != offline_smoke_results[1]["receipt"]:
        raise RuntimeError("Public and local package offline CreateChannel receipts differ")
    evidence["offline_activity_smoke"] = offline_smoke_results

    source_consumer_dir = output / "consumers" / "source-project-reference" / "net10.0"
    source_consumer_dir.mkdir(parents=True)
    source_project = consumer_project(
        source_consumer_dir,
        "net10.0",
        project_reference=project,
    )
    source_config = source_consumer_dir / "NuGet.Config"
    write_nuget_config(source_config, source_feeds, source_mapping)
    env["NUGET_PACKAGES"] = str(cache_root / "source-project-reference")
    source_log = output / "logs/consumer-source-project-reference-net10.0.log"
    evidence["commands"].append(
        run(
            [
                "dotnet", "restore", str(source_project), "--configfile", str(source_config),
                "-p:UseProjectReferences=true", "-p:TargetFrameworks=net10.0",
            ],
            cwd=source_consumer_dir,
            env=env,
            log=output / "logs/consumer-source-project-reference-restore.log",
        )
    )
    evidence["commands"].append(
        run(
            [
                "dotnet", "run", "--no-restore", "--project", str(source_project), "--framework", "net10.0",
                "-p:UseProjectReferences=true",
            ],
            cwd=source_consumer_dir,
            env=env,
            log=source_log,
        )
    )
    source_marker = next(
        (line.split("ELSA_ACTIVITY_DESCRIPTOR=", 1)[1] for line in source_log.read_text().splitlines() if "ELSA_ACTIVITY_DESCRIPTOR=" in line),
        None,
    )
    if not source_marker:
        raise RuntimeError(f"Project-reference consumer did not emit a runtime activity descriptor; see {source_log}")
    source_descriptor = json.loads(source_marker)
    released_net10 = next(
        item["descriptor"]
        for item in consumer_results
        if item["artifact"] == "public-3.8.4" and item["framework"] == "net10.0"
    )
    if source_descriptor != released_net10:
        raise RuntimeError(f"Source project-reference descriptor {source_descriptor} differs from public 3.8.4 {released_net10}")
    evidence["source_project_reference_consumer"] = {
        "framework": "net10.0",
        "result": "passed",
        "descriptor": source_descriptor,
        "graph": "Slack project -> Core 3.8.4 source project; distinct from both NuGet package consumers",
    }

    expected_commit = git_value(extensions_source, "rev-parse", "HEAD")
    if expected_commit != EXTENSIONS_SHA:
        raise RuntimeError("Extensions source worktree moved while proof was running")
    evidence["result"] = "passed"
    (output / "evidence.json").write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"result": evidence["result"], "evidence": str(output / "evidence.json")}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

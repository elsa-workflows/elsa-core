#!/usr/bin/env python3
"""Check that .github/workflows/packages.yml can publish only through its three gated jobs.

Parses the workflow (PyYAML) and fails when a publication capability - Feedz or NuGet secrets, NuGet
login, package pushes, Pages deployment, OIDC tokens or environments - appears outside
publish_preview_feedz, publish_nuget and deploy_coverage, or when one of those jobs can start without a
validated, explicit workflow_dispatch opt-in.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path
from typing import Any, Iterator

try:
    import yaml
except ImportError:
    sys.exit("PyYAML is required: python3 -m pip install PyYAML")

from validate_packages_publication_selection import COVERAGE_REFS, selection_errors

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/packages.yml"
CANDIDATE_WORKFLOW = ROOT / ".github/workflows/publish-consolidated-original-candidate.yml"
CANDIDATE_PUBLISH = "consolidated_package_executor.py publish"

SELECTION_JOB = "validate_publication_selection"
# Each gated job is switched on by the workflow_dispatch input of the same name.
GATED_JOBS = ("publish_preview_feedz", "publish_nuget", "publish_npm", "deploy_coverage")
MANUAL_DISPATCH = "github.event_name == 'workflow_dispatch'"
# Top-level conjuncts a gated job's if: needs beyond the dispatch and its own input.
REQUIRED_REF_CONDITIONS = {
    "publish_preview_feedz": (
        "startsWith(github.ref, 'refs/heads/') || (startsWith(github.ref, 'refs/tags/3.10.') && contains(github.ref_name, '-'))",
    ),
    "publish_nuget": (
        "startsWith(github.ref, 'refs/tags/')",
    ),
    "publish_npm": (
        "startsWith(github.ref, 'refs/tags/')",
    ),
    "deploy_coverage": (" || ".join(f"github.ref == '{ref}'" for ref in COVERAGE_REFS),),
}
SELECTION_COMMAND = "python3 scripts/validate_packages_publication_selection.py"
SELECTION_ENV = {
    "REF": "${{ github.ref }}",
    "PUBLISH_PREVIEW_FEEDZ": "${{ inputs.publish_preview_feedz }}",
    "PUBLISH_NUGET": "${{ inputs.publish_nuget }}",
    "PUBLISH_NPM": "${{ inputs.publish_npm }}",
    "DEPLOY_COVERAGE": "${{ inputs.deploy_coverage }}",
    "EXCLUDE_IDS": "${{ inputs.exclude_ids }}",
}
# Probed against the script the selection job runs, not a copy of its rules.
REQUIRED_REJECTIONS = (
    ("a v-prefixed tag", "refs/tags/v3.10.0", {}),
    ("a non-release tag", "refs/tags/test-1", {}),
    ("Feedz from a stable 3.10.x tag", "refs/tags/3.10.0", {"feedz": True}),
    ("Feedz from a later stable 3.10.x tag", "refs/tags/3.10.1", {"feedz": True}),
    ("npm from a stable 3.10.x tag", "refs/tags/3.10.0", {"npm": True}),
    ("exclude_ids on a prerelease tag", "refs/tags/3.10.0-rc1", {"nuget": True, "exclude_ids": "Elsa"}),
    ("exclude_ids on a non-3.10 tag", "refs/tags/3.8.5", {"nuget": True, "exclude_ids": "Elsa"}),
    ("exclude_ids when publish_nuget is false", "refs/tags/3.10.0", {"exclude_ids": "Elsa"}),
)
REQUIRED_ACCEPTANCES = (
    ("Feedz from a 3.10.x prerelease tag", "refs/tags/3.10.0-rc1", {"feedz": True}),
    ("NuGet from a stable 3.10.x tag", "refs/tags/3.10.0", {"nuget": True}),
    ("npm from a 3.10.x prerelease tag", "refs/tags/3.10.0-rc1", {"npm": True}),
    ("exclude_ids on a stable 3.10 nuget dispatch", "refs/tags/3.10.0", {"nuget": True, "exclude_ids": "Elsa"}),
)
PROMOTE_STABLE_REF = "startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')"
TAG_3100 = "github.ref == 'refs/tags/3.10.0'"
# Exact if: bodies after ${{ }} unwrap. Substring checks miss polarity
# (inverted Compile+Pack) and a leading `false &&` (disabled staging).
SKIP_STABLE_310_PROMOTE = (
    "!(github.event_name == 'workflow_dispatch' && inputs.publish_nuget && "
    f"{PROMOTE_STABLE_REF})"
)
SKIP_STABLE_310_PACK = (
    "!((github.event_name == 'workflow_dispatch' && inputs.publish_nuget && "
    f"{PROMOTE_STABLE_REF}) || ((github.event_name == 'release' || "
    f"github.event_name == 'workflow_dispatch') && {TAG_3100}))"
)
CANDIDATE_PACK = (
    f"(github.event_name == 'release' || github.event_name == 'workflow_dispatch') && "
    f"{TAG_3100} && !(github.event_name == 'workflow_dispatch' && inputs.publish_nuget)"
)
PROMOTE_STABLE_310 = PROMOTE_STABLE_REF
SKIP_STABLE_310_DOWNLOAD = f"!({PROMOTE_STABLE_REF})"
UPLOAD_PACKAGES = (
    "(github.event_name == 'release' || github.event_name == 'push' || "
    "(github.event_name == 'workflow_dispatch' && (inputs.publish_preview_feedz || inputs.publish_nuget))) && "
    "!(github.event_name == 'workflow_dispatch' && inputs.publish_nuget && "
    f"{PROMOTE_STABLE_REF})"
)
RETRIEVE_CANDIDATE = "python3 scripts/stage_consolidated_candidate_nupkgs.py retrieve"
STAGE_CANDIDATE = "python3 scripts/stage_consolidated_candidate_nupkgs.py stage"
GUARD_CANDIDATE = "python3 scripts/validate_packages_id_set.py"
PREPARE_CANDIDATE = "python3 scripts/integration-program/prepare_consolidated_release_candidate.py"
CANDIDATE_PACK_LINES = (
    "set -euo pipefail",
    f"{PREPARE_CANDIDATE} --version 3.10.0 --output \"$RUNNER_TEMP/consolidated-candidate\" --remote-sources",
    GUARD_CANDIDATE,
)
NPM_PAIR = "python3 scripts/integration-program/prove_studio_npm_pair.py"
BUILD_NPM_JOB = "build_npm"
SETUP_DOTNET_PIN = "actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9"
DOTNET_SDK = "10.0.300"
NPM_ARTIFACT = "studio-npm-pair"
WASM_TGZ = "elsa-workflows-elsa-studio-wasm-${GITHUB_REF_NAME}.tgz"
REACT_TGZ = "elsa-workflows-elsa-studio-wasm-react-${GITHUB_REF_NAME}.tgz"
ID_SET = ROOT / "scripts/validate_packages_id_set.txt"
EXPECTED_PACKAGE_COUNT = 231
# Exact non-empty run: lines after horizontal-whitespace collapse. A joined
# string would accept YAML `>-`, which folds the block into one no-op `set`.
CANDIDATE_GUARD_LINES = ("set -euo pipefail", STAGE_CANDIDATE, GUARD_CANDIDATE)
NUGET_GUARD_LINES = (STAGE_CANDIDATE, GUARD_CANDIDATE)
CANDIDATE_GUARD_REF = "refs/tags/3.10.0"
GUARD_STEP_KEYS = frozenset({"name", "env", "run", "id", "if", "continue-on-error"})
FORBIDDEN_PACK_ROUTE = (
    ("CONSOLIDATED_RELEASE_CANDIDATE", "packages.yml must not set CONSOLIDATED_RELEASE_CANDIDATE; the candidate workflow owns that pack"),
    ("ConsolidatedReleaseCandidate:", "packages.yml must not pack ConsolidatedReleaseCandidate; the candidate workflow owns that pack"),
    ("build_studio_clientlibs.sh", "packages.yml must not build Studio ClientLibs; the candidate workflow owns that build"),
)
# Capabilities only a gated job may reference. Expression contexts and action names are case-insensitive.
PUBLICATION_REFERENCES = {
    "secrets.FEEDZ*": re.compile(r"\bsecrets\s*(?:\.|\[\s*')\s*feedz", re.IGNORECASE),
    "secrets.NUGET*": re.compile(r"\bsecrets\s*(?:\.|\[\s*')\s*nuget", re.IGNORECASE),
    "NuGet/login": re.compile(r"\bnuget/login\b", re.IGNORECASE),
    "actions/deploy-pages": re.compile(r"\bactions/deploy-pages\b", re.IGNORECASE),
    "dotnet nuget push": re.compile(r"\bdotnet[\s\\]+nuget[\s\\]+push\b", re.IGNORECASE),
    "the push-nuget-packages action": re.compile(r"push-nuget-packages", re.IGNORECASE),
    "npm publish": re.compile(r"\bnpm[\s\\]+publish\b", re.IGNORECASE),
}
EXPRESSION = re.compile(r"\$\{\{(.*?)\}\}", re.DOTALL)
# Inside ${{ }}: the secrets context as a whole, e.g. toJSON(secrets), which carries every feed key.
WHOLE_SECRETS = re.compile(r"\bsecrets\b(?!\s*[.\[])", re.IGNORECASE)
# Inside ${{ }}: secrets accessed by index, e.g. secrets[vars.X] or secrets['FEEDZ_API_KEY']. The literal
# name patterns above already flag indices that spell out a feed key, but any index - computed or not -
# can resolve to a publication secret, so indexed access is never allowed outside the gated jobs.
INDEXED_SECRETS = re.compile(r"\bsecrets\s*\[", re.IGNORECASE)
# A GitHub-expression single-quoted string literal, '' being the escaped quote inside one.
STRING_LITERAL = re.compile(r"'(?:[^']|'')*'")


def _blank_string_literals(expression: str) -> str:
    """Blank out string literals so INDEXED_SECRETS scans code, not quoted text that merely contains
    the substring "secrets[", e.g. the literal 'secrets[disabled]' in an unrelated if: condition. A
    real indexed access such as secrets['FEEDZ_API_KEY'] still becomes secrets[''] and still matches."""
    return STRING_LITERAL.sub("''", expression)
# Status functions replace the implicit success() that makes a job wait for its needs to succeed.
STATUS_FUNCTION = re.compile(r"\b(?:always|failure|cancelled)\s*\(")


def triggers(workflow: dict) -> dict:
    # YAML 1.1 reads the bare key `on` as True.
    value = workflow.get("on", workflow.get(True))
    if isinstance(value, str):
        return {value: None}
    if isinstance(value, list):
        return dict.fromkeys(value)
    return value if isinstance(value, dict) else {}


def strings(node: Any) -> Iterator[str]:
    if isinstance(node, dict):
        for key, value in node.items():
            if isinstance(key, str):
                yield key
            yield from strings(value)
    elif isinstance(node, list):
        for item in node:
            yield from strings(item)
    elif isinstance(node, str):
        yield node


def needs(job: dict) -> set[str]:
    value = job.get("needs") or []
    return {value} if isinstance(value, str) else set(value)


def grants_id_token(permissions: Any) -> bool:
    if isinstance(permissions, str):
        return permissions.strip().lower() == "write-all"
    return isinstance(permissions, dict) and str(permissions.get("id-token", "")).lower() == "write"


def if_expression(value: Any) -> str | None:
    """The expression an if: evaluates; None when text around ${{ }} makes it an always-true string."""
    text = "" if value is None else " ".join(str(value).split())
    if text.startswith("${{") and text.endswith("}}") and text.count("${{") == 1:
        return text[3:-2].strip()
    return None if "${{" in text else text


def _depths(expression: str) -> Iterator[tuple[int, int]]:
    """Yield (index, parenthesis depth) for each character outside string literals."""
    depth, quoted = 0, False
    for index, char in enumerate(expression):
        if char == "'":
            quoted = not quoted
        elif not quoted:
            yield index, depth
            depth += (char == "(") - (char == ")")


def unwrap(expression: str) -> str:
    """Drop parentheses that enclose the whole expression."""
    while expression.startswith("(") and all(depth > 0 for index, depth in _depths(expression) if index):
        expression = expression[1:-1].strip()
    return expression


def split_top_level(expression: str, operator: str) -> list[str]:
    parts, start = [], 0
    for index, depth in _depths(expression):
        if depth == 0 and index >= start and expression.startswith(operator, index):
            parts.append(unwrap(expression[start:index].strip()))
            start = index + len(operator)
    parts.append(unwrap(expression[start:].strip()))
    return parts


def ref_condition_accepts(value: Any, ref: str) -> bool | None:
    """Evaluate an if: made of github.ref == '...' and startsWith(github.ref, '...') alternatives."""
    if value is None:
        return True
    expression = if_expression(value)
    if expression is None:
        return None
    accepted = []
    for term in split_top_level(unwrap(expression), "||"):
        if match := re.fullmatch(r"github\.ref == '([^']*)'", term):
            accepted.append(ref == match[1])
        elif match := re.fullmatch(r"startsWith\(github\.ref, '([^']*)'\)", term):
            accepted.append(ref.startswith(match[1]))
        else:
            return None
    return any(accepted)


def trigger_violations(workflow: dict) -> list[str]:
    on = triggers(workflow)
    violations = []
    if "workflow_call" in on:
        violations.append("the workflow must not be callable (workflow_call); a caller could supply the publication inputs")
    inputs = (on.get("workflow_dispatch") or {}).get("inputs") or {}
    for name in GATED_JOBS:
        spec = inputs.get(name)
        if not isinstance(spec, dict) or spec.get("type") != "boolean":
            violations.append(f"workflow_dispatch input {name!r} must be a boolean")
    for name, spec in inputs.items():
        if isinstance(spec, dict) and spec.get("type") == "boolean" and spec.get("default") is not False:
            violations.append(f"workflow_dispatch input {name!r} must default to false")
    return violations


def capability_violations(workflow: dict) -> list[str]:
    scopes = {"the workflow outside its jobs": {key: value for key, value in workflow.items() if key != "jobs"}}
    scopes.update(
        (f"job {name!r}", job)
        for name, job in workflow["jobs"].items()
        if name not in GATED_JOBS and isinstance(job, dict)
    )
    violations = []
    for scope, node in scopes.items():
        texts = list(strings(node))
        found = [label for label, pattern in PUBLICATION_REFERENCES.items() if any(map(pattern.search, texts))]
        expressions = [expression for text in texts for expression in EXPRESSION.findall(text)]
        if any(WHOLE_SECRETS.search(expression) for expression in expressions):
            found.append("the whole secrets context")
        if any(INDEXED_SECRETS.search(_blank_string_literals(expression)) for expression in expressions):
            found.append("an indexed secrets access")
        if grants_id_token(node.get("permissions")):
            found.append("id-token: write")
        if "environment" in node:
            found.append("an environment")
        if "secrets" in node:
            found.append("secrets passed to a reusable workflow")
        violations += [f"{scope} references {label}; only {', '.join(GATED_JOBS)} may" for label in found]
    return violations


def gated_job_violations(jobs: dict) -> list[str]:
    violations = []
    for name in GATED_JOBS:
        job = jobs.get(name)
        if not isinstance(job, dict):
            violations.append(f"gated job {name!r} is missing")
            continue
        if SELECTION_JOB not in needs(job):
            violations.append(f"{name} must need {SELECTION_JOB} so a rejected selection skips it")
        expression = if_expression(job.get("if"))
        if expression is None:
            violations.append(f"{name} if: has text outside ${{{{ }}}}, which GitHub treats as an always-true string")
            continue
        if STATUS_FUNCTION.search(expression):
            violations.append(f"{name} if: must not call always(), failure() or cancelled(); they run it even when {SELECTION_JOB} fails")
        conjuncts = split_top_level(unwrap(expression), "&&")
        for required in (MANUAL_DISPATCH, f"inputs.{name}", *REQUIRED_REF_CONDITIONS.get(name, ())):
            if required not in conjuncts:
                violations.append(f"{name} if: must require {required}")
    return violations


BUILD_JOB = "build"
BUILD_SELECTION_GATE = f"github.event_name != 'workflow_dispatch' || needs.{SELECTION_JOB}.result == 'success'"


def build_gate_violations(jobs: dict) -> list[str]:
    """A dispatch the selection check rejects must not build or upload package artifacts."""
    job = jobs.get(BUILD_JOB)
    if not isinstance(job, dict):
        return [f"job {BUILD_JOB!r} is missing"]
    violations = []
    if SELECTION_JOB not in needs(job):
        violations.append(f"{BUILD_JOB} must need {SELECTION_JOB} so a rejected dispatch does not build")
    expression = if_expression(job.get("if"))
    if expression is None:
        violations.append(f"{BUILD_JOB} if: has text outside ${{{{ }}}}, which GitHub treats as an always-true string")
    elif BUILD_SELECTION_GATE not in split_top_level(unwrap(expression), "&&"):
        violations.append(f"{BUILD_JOB} if: must require ({BUILD_SELECTION_GATE})")
    return violations


def selection_violations(jobs: dict) -> list[str]:
    job = jobs.get(SELECTION_JOB)
    if not isinstance(job, dict):
        return [f"job {SELECTION_JOB!r} is missing"]
    violations = []
    steps = [step for step in job.get("steps") or [] if isinstance(step, dict)]
    runs = [step for step in steps if " ".join(str(step.get("run", "")).split()) == SELECTION_COMMAND]
    if len(runs) != 1:
        violations.append(f"{SELECTION_JOB} must run exactly `{SELECTION_COMMAND}` in one step")
    else:
        env = {key: " ".join(str(value).split()) for key, value in (runs[0].get("env") or {}).items()}
        for key, value in SELECTION_ENV.items():
            if env.get(key) != value:
                violations.append(f"{SELECTION_JOB} must pass {key}: {value} to the selection script")
    if any(node.get("continue-on-error") not in (None, False) for node in (job, *steps)):
        violations.append(f"{SELECTION_JOB} must not continue on error; a rejected selection has to fail the job")
    for description, ref, selected in REQUIRED_REJECTIONS:
        if not selection_errors(ref, **selected):
            violations.append(f"the selection script must reject {description}")
    for description, ref, selected in REQUIRED_ACCEPTANCES:
        if selection_errors(ref, **selected):
            violations.append(f"the selection script must accept {description}")
    return violations


def pages_upload_violations(jobs: dict) -> list[str]:
    conditions = [
        step.get("if")
        for name, job in jobs.items()
        if name not in GATED_JOBS and isinstance(job, dict)
        for step in job.get("steps") or []
        if isinstance(step, dict) and str(step.get("uses", "")).lower().startswith("actions/upload-pages-artifact")
    ]
    if not conditions:
        return ["no step uploads the Pages artifact that deploy_coverage deploys"]
    violations = []
    for ref in COVERAGE_REFS:
        results = [ref_condition_accepts(condition, ref) for condition in conditions]
        if None in results:
            return ["the Upload Pages artifact condition must be github.ref == '...' or startsWith(github.ref, '...') alternatives"]
        if not any(results):
            violations.append(f"deploy_coverage accepts {ref}, but no Pages artifact is uploaded for it")
    return violations


def _step_run(step: dict) -> str:
    return " ".join(str(step.get("run") or "").split())


def _run_lines(step: dict) -> list[str]:
    """Whitespace-normalized non-empty lines of a step's run: body."""
    return [" ".join(line.split()) for line in str(step.get("run") or "").splitlines() if line.strip()]


def _has_defaults_run_shell(node: Any) -> bool:
    if not isinstance(node, dict):
        return False
    defaults = node.get("defaults")
    if not isinstance(defaults, dict):
        return False
    run = defaults.get("run")
    return isinstance(run, dict) and "shell" in run


def _guard_step_extra_keys(step: dict) -> list[str]:
    return sorted(set(step) - GUARD_STEP_KEYS)


def _unique_step(steps: list[dict], match, *, missing: str | None, duplicate: str) -> tuple[int | None, list[str]]:
    """Index of the only matching step. missing=None means optional (zero or one)."""
    found = [index for index, step in enumerate(steps) if match(step)]
    if len(found) > 1:
        return None, [duplicate]
    if not found:
        return None, [missing] if missing else []
    return found[0], []


def promotion_job_violations(jobs: dict) -> list[str]:
    """Stable 3.10.x nuget.org must promote pinned candidate bytes, never a rebuild."""
    violations = []
    job = jobs.get("publish_nuget")
    if not isinstance(job, dict):
        return ["gated job 'publish_nuget' is missing"]
    steps = [step for step in job.get("steps") or [] if isinstance(step, dict)]
    if any(node.get("continue-on-error") not in (None, False) for node in (job, *steps)):
        violations.append("publish_nuget must not continue on error; a failed candidate guard must block the push")
    retrieve_missing = "publish_nuget must retrieve the pinned candidate, stage it, and run the ID-set guard before NuGet login"
    retrieve, extra = _unique_step(
        steps,
        lambda step: RETRIEVE_CANDIDATE in _step_run(step),
        missing=retrieve_missing,
        duplicate="publish_nuget must contain exactly one candidate retrieve step",
    )
    violations += extra
    stage, extra = _unique_step(
        steps,
        lambda step: STAGE_CANDIDATE in _step_run(step),
        missing=retrieve_missing,
        duplicate="publish_nuget must contain exactly one candidate staging step",
    )
    violations += extra
    guard, extra = _unique_step(
        steps,
        lambda step: GUARD_CANDIDATE in _step_run(step),
        missing=retrieve_missing,
        duplicate="publish_nuget must contain exactly one candidate guard step",
    )
    violations += extra
    login = next(
        (index for index, step in enumerate(steps) if "nuget/login" in str(step.get("uses") or "").lower()),
        None,
    )
    if login is None and None not in (retrieve, stage, guard):
        violations.append(retrieve_missing)
    elif None not in (retrieve, stage, guard, login) and not retrieve < stage <= guard < login:
        violations.append("publish_nuget must retrieve, stage, and guard the pinned candidate before NuGet login")
    if guard is not None:
        if _run_lines(steps[guard]) != list(NUGET_GUARD_LINES):
            violations.append("publish_nuget candidate staging run: must be exactly stage and the ID-set guard")
        if _guard_step_extra_keys(steps[guard]):
            violations.append(
                "publish_nuget candidate staging keys must be among name, env, run, id, if, continue-on-error"
            )
    if _has_defaults_run_shell(job):
        violations.append("publish_nuget must not set defaults.run.shell")
    for index, expected, message in (
        (retrieve, PROMOTE_STABLE_310, "publish_nuget candidate retrieve if: must be exactly the stable 3.10.x tag condition"),
        (stage, PROMOTE_STABLE_310, "publish_nuget candidate staging if: must be exactly the stable 3.10.x tag condition"),
        (guard, PROMOTE_STABLE_310, "publish_nuget candidate guard if: must be exactly the stable 3.10.x tag condition"),
    ):
        if index is None:
            continue
        expression = if_expression(steps[index].get("if"))
        if expression != expected:
            violations.append(message)
        if STATUS_FUNCTION.search(expression or ""):
            violations.append("publish_nuget candidate promotion steps must not call always(), failure() or cancelled()")
    download, extra = _unique_step(
        steps,
        lambda step: step.get("name") == "Download Packages",
        missing="publish_nuget must contain Download Packages",
        duplicate="publish_nuget must contain exactly one Download Packages step",
    )
    violations += extra
    if download is not None and if_expression(steps[download].get("if")) != SKIP_STABLE_310_DOWNLOAD:
        violations.append("publish_nuget Download Packages if: must skip stable 3.10.x tags")
    build = jobs.get(BUILD_JOB)
    build_steps = [step for step in (build or {}).get("steps") or [] if isinstance(step, dict)] if isinstance(build, dict) else []
    compile_step, extra = _unique_step(
        build_steps,
        lambda step: step.get("name") == "Compile+Pack",
        missing="build job must still contain Compile+Pack for non-promote publication",
        duplicate="build job must contain exactly one Compile+Pack step",
    )
    violations += extra
    setup_dotnet, extra = _unique_step(
        build_steps,
        lambda step: "setup-dotnet" in str(step.get("uses") or "").lower(),
        missing="build job must contain setup-dotnet",
        duplicate="build job must contain exactly one setup-dotnet step",
    )
    violations += extra
    # Node is only for main's React Studio assets; release/3.10.0 does not have it.
    setup_node, extra = _unique_step(
        build_steps,
        lambda step: step.get("name") == "Set up Node for React Studio assets",
        missing=None,
        duplicate="build job must contain at most one Node setup step",
    )
    violations += extra
    upload, extra = _unique_step(
        build_steps,
        lambda step: step.get("name") == "Upload artifact",
        missing="build job must contain Upload artifact",
        duplicate="build job must contain exactly one Upload artifact step",
    )
    violations += extra
    if compile_step is not None and if_expression(build_steps[compile_step].get("if")) != SKIP_STABLE_310_PACK:
        violations.append("build Compile+Pack must skip a stable 3.10.x nuget.org promotion and the 3.10.0 candidate pack")
    if setup_dotnet is not None and if_expression(build_steps[setup_dotnet].get("if")) != SKIP_STABLE_310_PROMOTE:
        violations.append("build setup-dotnet must skip a stable 3.10.x nuget.org promotion")
    if setup_node is not None and if_expression(build_steps[setup_node].get("if")) != SKIP_STABLE_310_PROMOTE:
        violations.append("build Node setup must skip a stable 3.10.x nuget.org promotion")
    if upload is not None and if_expression(build_steps[upload].get("if")) != UPLOAD_PACKAGES:
        violations.append("build Upload artifact must skip a stable 3.10.x nuget.org promotion")
    pack, extra = _unique_step(
        build_steps,
        lambda step: PREPARE_CANDIDATE in _step_run(step),
        missing="build job must pack the 3.10.0 set via prepare_consolidated_release_candidate.py",
        duplicate="build job must contain exactly one consolidated candidate pack step",
    )
    violations += extra
    if pack is not None:
        if if_expression(build_steps[pack].get("if")) != CANDIDATE_PACK:
            violations.append("build candidate pack if: must be the 3.10.0 tag pack condition")
        if _run_lines(build_steps[pack]) != list(CANDIDATE_PACK_LINES):
            violations.append("build candidate pack run: must be exactly prepare, then the ID-set guard")
        if GUARD_CANDIDATE not in _step_run(build_steps[pack]):
            violations.append("build candidate pack must run the ID-set guard")
    return violations


def id_set_count_violations() -> list[str]:
    ids = [
        line.strip()
        for line in ID_SET.read_text(encoding="utf-8").splitlines()
        if line.strip() and not line.startswith("#")
    ]
    if len(ids) != EXPECTED_PACKAGE_COUNT:
        return [f"validate_packages_id_set.txt must list exactly {EXPECTED_PACKAGE_COUNT} package IDs"]
    return []


def _dotnet_version(step: dict) -> str:
    value = (step.get("with") or {}).get("dotnet-version") if isinstance(step.get("with"), dict) else None
    if isinstance(value, list):
        value = "\n".join(str(item) for item in value)
    return " ".join(str(value or "").split())


def build_npm_violations(jobs: dict) -> list[str]:
    """The Studio npm pair is built without credentials and uploaded for publish_npm."""
    job = jobs.get(BUILD_NPM_JOB)
    if not isinstance(job, dict):
        return ["job 'build_npm' is missing"]
    violations = []
    if BUILD_JOB not in needs(job):
        violations.append("build_npm must need build")
    if SELECTION_JOB not in needs(job):
        violations.append("build_npm must need validate_publication_selection")
    env = job.get("env") if isinstance(job.get("env"), dict) else {}
    if str(env.get("PYTHONDONTWRITEBYTECODE") or "") != "1":
        violations.append("build_npm must set PYTHONDONTWRITEBYTECODE to 1")
    if grants_id_token(job.get("permissions")):
        violations.append("build_npm must not request id-token: write")
    steps = [step for step in job.get("steps") or [] if isinstance(step, dict)]
    if any(node.get("continue-on-error") not in (None, False) for node in (job, *steps)):
        violations.append("build_npm must not continue on error")
    if any(re.search(r"\bnpm[\s\\]+publish\b", _step_run(step), re.IGNORECASE) for step in steps):
        violations.append("build_npm must not publish")
    setup, extra = _unique_step(
        steps,
        lambda step: str(step.get("uses") or "").startswith("actions/setup-dotnet@"),
        missing="build_npm must pin setup-dotnet to 10.0.300",
        duplicate="build_npm must contain exactly one setup-dotnet step",
    )
    violations += extra
    if setup is not None:
        uses = str(steps[setup].get("uses") or "")
        if uses != SETUP_DOTNET_PIN:
            violations.append("build_npm must pin setup-dotnet to the studio-npm-pair SHA")
        if _dotnet_version(steps[setup]) != DOTNET_SDK:
            violations.append("build_npm must pin setup-dotnet to 10.0.300")
    build_pair, extra = _unique_step(
        steps,
        lambda step: NPM_PAIR in _step_run(step),
        missing="build_npm must build the Studio pair via prove_studio_npm_pair.py",
        duplicate="build_npm must contain exactly one Studio pair build step",
    )
    violations += extra
    upload, extra = _unique_step(
        steps,
        lambda step: str(step.get("uses") or "").startswith("actions/upload-artifact@")
        and str((step.get("with") or {}).get("name") or "") == NPM_ARTIFACT,
        missing="build_npm must upload the studio-npm-pair artifact",
        duplicate="build_npm must contain exactly one studio-npm-pair upload",
    )
    violations += extra
    if None not in (build_pair, upload) and not build_pair < upload:
        violations.append("build_npm must build the pair before uploading it")
    return violations


def npm_job_violations(jobs: dict) -> list[str]:
    """The Studio npm publisher downloads the pair and publishes only to Feedz."""
    job = jobs.get("publish_npm")
    if not isinstance(job, dict):
        return ["gated job 'publish_npm' is missing"]
    violations = []
    if BUILD_NPM_JOB not in needs(job):
        violations.append("publish_npm must need build_npm so it publishes the credential-free pair artifact")
    if grants_id_token(job.get("permissions")):
        violations.append("publish_npm must not request id-token: write until trusted publishing is confirmed")
    steps = [step for step in job.get("steps") or [] if isinstance(step, dict)]
    if any(node.get("continue-on-error") not in (None, False) for node in (job, *steps)):
        violations.append("publish_npm must not continue on error")
    if any(NPM_PAIR in _step_run(step) for step in steps):
        violations.append("publish_npm must not build the Studio pair; build_npm owns that step")
    if any(str(step.get("uses") or "").startswith("actions/setup-dotnet@") for step in steps):
        violations.append("publish_npm must not install a .NET SDK; build_npm owns that step")
    closed, extra = _unique_step(
        steps,
        lambda step: step.get("name") == "Fail closed on stable 3.10.x",
        missing="publish_npm must fail closed on a stable 3.10.x tag",
        duplicate="publish_npm must contain exactly one stable 3.10.x fail-closed step",
    )
    violations += extra
    if closed is not None:
        if if_expression(steps[closed].get("if")) != PROMOTE_STABLE_310:
            violations.append("publish_npm fail-closed if: must be exactly the stable 3.10.x tag condition")
        if "exit 1" not in _step_run(steps[closed]):
            violations.append("publish_npm fail-closed step must exit 1")
        if STATUS_FUNCTION.search(if_expression(steps[closed].get("if")) or ""):
            violations.append("publish_npm fail-closed step must not call always(), failure() or cancelled()")
    download, extra = _unique_step(
        steps,
        lambda step: str(step.get("uses") or "").startswith("actions/download-artifact@")
        and str((step.get("with") or {}).get("name") or "") == NPM_ARTIFACT,
        missing="publish_npm must download the studio-npm-pair artifact",
        duplicate="publish_npm must contain exactly one studio-npm-pair download",
    )
    violations += extra
    verify, extra = _unique_step(
        steps,
        lambda step: WASM_TGZ in _step_run(step) and REACT_TGZ in _step_run(step),
        missing="publish_npm must verify the two Studio npm tarballs at the tag version",
        duplicate="publish_npm must contain exactly one Studio npm pair verify step",
    )
    violations += extra
    feedz_publish, extra = _unique_step(
        steps,
        lambda step: step.get("name") == "npm publish to Feedz",
        missing="publish_npm must contain npm publish to Feedz",
        duplicate="publish_npm must contain exactly one Feedz npm publish step",
    )
    violations += extra
    if feedz_publish is not None:
        run = _step_run(steps[feedz_publish])
        if not re.search(r"\bnpm[\s\\]+publish\b.*--tag", run):
            violations.append("publish_npm Feedz publish must pass a dist-tag")
        if "EPUBLISHCONFLICT" not in run or "cannot publish over" not in run:
            violations.append("publish_npm Feedz publish must skip a version that already exists")
        if "::error::" not in run:
            violations.append("publish_npm Feedz publish must fail closed when the Feedz key is empty")
    publishes = [index for index, step in enumerate(steps) if re.search(r"\bnpm[\s\\]+publish\b", _step_run(step), re.IGNORECASE)]
    if len(publishes) != 1:
        violations.append("publish_npm must contain exactly one npm publish step")
    if any(step.get("name") == "npm publish to npmjs.com" or re.search(r"registry\.npmjs\.(org|com)", _step_run(step)) for step in steps):
        violations.append("publish_npm must not publish to npmjs.com until trusted publishing is confirmed")
    if None not in (closed, download, verify, feedz_publish) and not closed < download < verify < feedz_publish:
        violations.append("publish_npm must fail closed on stable 3.10.x, download and verify the pair, then publish to Feedz")
    return violations


def promotion_source_violations(source: str) -> list[str]:
    return [message for needle, message in FORBIDDEN_PACK_ROUTE if needle in source]


def candidate_publisher_violations(source: str) -> list[str]:
    """The candidate Feedz publisher must run the same 231-ID guard before any push."""
    violations = []
    lowered = source.lower()
    for marker in ("skip_id_set", "skip_guard", "bypass_id_set", "bypass_guard", "expected_ids"):
        if marker in lowered:
            violations.append(
                "publish-consolidated-original-candidate.yml must not offer a skip, bypass, or ID-list override"
            )
    guard = source.find(GUARD_CANDIDATE)
    publish = source.find(CANDIDATE_PUBLISH)
    if guard < 0:
        violations.append(
            "publish-consolidated-original-candidate.yml must run the same ID-set guard before Feedz"
        )
    elif publish < 0 or guard > publish:
        violations.append(
            "publish-consolidated-original-candidate.yml must run the ID-set guard before the Feedz publish"
        )
    workflow = yaml.safe_load(source)
    jobs = workflow.get("jobs") if isinstance(workflow, dict) else None
    job = jobs.get("publish") if isinstance(jobs, dict) else None
    steps = [step for step in (job or {}).get("steps") or [] if isinstance(step, dict)] if isinstance(job, dict) else []
    guard_steps = [step for step in steps if GUARD_CANDIDATE in _step_run(step)]
    if not guard_steps and guard >= 0:
        violations.append(
            "publish-consolidated-original-candidate.yml must run the same ID-set guard before Feedz"
        )
    if _has_defaults_run_shell(workflow) or _has_defaults_run_shell(job):
        violations.append(
            "publish-consolidated-original-candidate.yml must not set defaults.run.shell"
        )
    if isinstance(job, dict) and "continue-on-error" in job and job.get("continue-on-error") is not False:
        violations.append(
            "publish-consolidated-original-candidate.yml must not continue on error"
        )
    for step in guard_steps:
        # Absent or continue-on-error: false is GitHub's default and the same
        # allowlist publish_nuget uses. Any other value, including 0 or '', is a
        # violation. An if: key always disables or conditions the guard, including
        # falsy values such as false, '' and 0.
        if "continue-on-error" in step and step.get("continue-on-error") is not False:
            violations.append(
                "publish-consolidated-original-candidate.yml must not continue on error"
            )
        if "if" in step:
            violations.append(
                "publish-consolidated-original-candidate.yml guard step must not have an if:"
            )
        if _guard_step_extra_keys(step):
            violations.append(
                "publish-consolidated-original-candidate.yml guard step keys must be among name, env, run, id, if, continue-on-error"
            )
        if _run_lines(step) != list(CANDIDATE_GUARD_LINES):
            violations.append(
                "publish-consolidated-original-candidate.yml guard run: must be exactly set -euo pipefail, stage, and the ID-set guard"
            )
        env = step.get("env") if isinstance(step.get("env"), dict) else {}
        if " ".join(str(env.get("REF") or "").split()) != CANDIDATE_GUARD_REF:
            violations.append(
                "publish-consolidated-original-candidate.yml guard env REF must be refs/tags/3.10.0"
            )
    # Recheck and publish must not be gated by always()/failure()/cancelled().
    # The evidence upload's if: always() is the one allowed status-function use.
    for step in steps:
        uses = str(step.get("uses") or "").lower()
        if "upload-artifact" in uses:
            continue
        if GUARD_CANDIDATE in _step_run(step):
            continue
        expression = if_expression(step.get("if")) if "if" in step else ""
        if STATUS_FUNCTION.search(expression or ""):
            violations.append(
                "publish-consolidated-original-candidate.yml recheck and publish steps must not call always(), failure() or cancelled()"
            )
    return violations


def workflow_violations(workflow: Any) -> list[str]:
    if not isinstance(workflow, dict) or not isinstance(workflow.get("jobs"), dict):
        return ["the workflow has no jobs mapping"]
    jobs = workflow["jobs"]
    defaults = ["the workflow must not set defaults.run.shell"] if _has_defaults_run_shell(workflow) else []
    return [
        *trigger_violations(workflow),
        *capability_violations(workflow),
        *gated_job_violations(jobs),
        *build_gate_violations(jobs),
        *selection_violations(jobs),
        *pages_upload_violations(jobs),
        *promotion_job_violations(jobs),
        *build_npm_violations(jobs),
        *npm_job_violations(jobs),
        *defaults,
    ]


def source_violations(source: str) -> list[str]:
    return [
        *workflow_violations(yaml.safe_load(source)),
        *promotion_source_violations(source),
    ]


def main() -> int:
    violations = [
        *(f"{ID_SET.relative_to(ROOT)}: {violation}" for violation in id_set_count_violations()),
        *(f"{WORKFLOW.relative_to(ROOT)}: {violation}" for violation in source_violations(WORKFLOW.read_text(encoding="utf-8"))),
        *(
            f"{CANDIDATE_WORKFLOW.relative_to(ROOT)}: {violation}"
            for violation in candidate_publisher_violations(CANDIDATE_WORKFLOW.read_text(encoding="utf-8"))
        ),
    ]
    for violation in violations:
        print(violation, file=sys.stderr)
    if violations:
        return 1
    print("Package publication gates hold: only the four default-off, validated dispatch jobs can publish or deploy.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

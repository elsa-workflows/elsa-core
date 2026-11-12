#!/usr/bin/env python3
"""Check that .github/workflows/packages.yml can publish only through its gated jobs.

Parses the workflow (PyYAML) and fails when a publication capability - Feedz or NuGet secrets, NuGet
login, package pushes, Pages deployment, OIDC tokens or environments - appears outside
publish_preview_feedz, publish_nuget, publish_npm, deploy_coverage and the two candidate-promotion publish
jobs, or when one of those jobs can start without a validated, explicit workflow_dispatch opt-in.
Candidate promotion (promote_candidate_version) has its own rules: see candidate_promotion_violations.
"""

from __future__ import annotations

import re
import shlex
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
    "PROMOTE_CANDIDATE_VERSION": "${{ inputs.promote_candidate_version }}",
    "PROMOTE_TARGET": "${{ inputs.promote_target }}",
}
GUARD_EXCLUDE_IDS = SELECTION_ENV["EXCLUDE_IDS"]
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
    ("candidate promotion from a release branch", "refs/heads/release/3.10.0", {"promote": "3.10.1"}),
    ("candidate promotion from a tag", "refs/tags/3.10.1", {"promote": "3.10.1"}),
    ("candidate promotion combined with publish_nuget", "refs/heads/main", {"promote": "3.10.1", "nuget": True}),
)
REQUIRED_ACCEPTANCES = (
    ("Feedz from a 3.10.x prerelease tag", "refs/tags/3.10.0-rc1", {"feedz": True}),
    ("NuGet from a stable 3.10.x tag", "refs/tags/3.10.0", {"nuget": True}),
    ("npm from a 3.10.x prerelease tag", "refs/tags/3.10.0-rc1", {"npm": True}),
    ("exclude_ids on a stable 3.10 nuget dispatch", "refs/tags/3.10.0", {"nuget": True, "exclude_ids": "Elsa"}),
    ("candidate promotion from main", "refs/heads/main", {"promote": "3.10.1"}),
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
NPM_PUBLISH_SCRIPT = "scripts/publish_npm_tarball.py"
WASM_PACKAGE = "@elsa-workflows/elsa-studio-wasm"
REACT_PACKAGE = "@elsa-workflows/elsa-studio-wasm-react"
FEEDZ_NPM_REGISTRY = "https://f.feedz.io/elsa-workflows/elsa-3/npm/"
ID_SET = ROOT / "scripts/validate_packages_id_set.txt"
EXPECTED_PACKAGE_COUNT = 231
# Exact non-empty run: lines after horizontal-whitespace collapse. A joined
# string would accept YAML `>-`, which folds the block into one no-op `set`.
NUGET_GUARD_LINES = (STAGE_CANDIDATE, GUARD_CANDIDATE)
GUARD_STEP_KEYS = frozenset({"name", "env", "run", "id", "if", "continue-on-error"})
FORBIDDEN_PACK_ROUTE = (
    ("CONSOLIDATED_RELEASE_CANDIDATE", "packages.yml must not set CONSOLIDATED_RELEASE_CANDIDATE; the candidate workflow owns that pack"),
    ("ConsolidatedReleaseCandidate:", "packages.yml must not pack ConsolidatedReleaseCandidate; the candidate workflow owns that pack"),
    ("build_studio_clientlibs.sh", "packages.yml must not build Studio ClientLibs; the candidate workflow owns that build"),
)
# Capabilities only a gated job may reference. Expression contexts and action names are case-insensitive.
PUBLICATION_REFERENCES = {
    "secrets.FEEDZ*": re.compile(r"\bsecrets\s*(?:\.|\[\s*')\s*feedz", re.IGNORECASE),
    "secrets.ELSA_CONSOLIDATED*": re.compile(r"\bsecrets\s*(?:\.|\[\s*')\s*elsa_consolidated", re.IGNORECASE),
    "secrets.NUGET*": re.compile(r"\bsecrets\s*(?:\.|\[\s*')\s*nuget", re.IGNORECASE),
    "NuGet/login": re.compile(r"\bnuget/login\b", re.IGNORECASE),
    "actions/deploy-pages": re.compile(r"\bactions/deploy-pages\b", re.IGNORECASE),
    "dotnet nuget push": re.compile(r"\bdotnet[\s\\]+nuget[\s\\]+push\b", re.IGNORECASE),
    "the push-nuget-packages action": re.compile(r"push-nuget-packages", re.IGNORECASE),
    "npm publish": re.compile(r"\bnpm[\s\\]+publish\b", re.IGNORECASE),
    "the npm tarball publisher script": re.compile(r"publish_npm_tarball\.py"),
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
        if name not in GATED_JOBS and name not in CANDIDATE_PUBLISH_JOBS and isinstance(job, dict)
    )
    violations = []
    for scope, node in scopes.items():
        run_texts = list(_run_strings(node))
        run_text_set = set(run_texts)
        other_texts = [text for text in strings(node) if text not in run_text_set]
        found = {label for label, pattern in PUBLICATION_REFERENCES.items() if any(map(pattern.search, other_texts))}
        for run in run_texts:
            found.update(_run_references(run))
        expressions = [expression for text in other_texts for expression in EXPRESSION.findall(text)]
        if any(WHOLE_SECRETS.search(expression) for expression in expressions):
            found.add("the whole secrets context")
        if any(INDEXED_SECRETS.search(_blank_string_literals(expression)) for expression in expressions):
            found.add("an indexed secrets access")
        if grants_id_token(node.get("permissions")):
            found.add("id-token: write")
        if "environment" in node:
            found.add("an environment")
        if "secrets" in node:
            found.add("secrets passed to a reusable workflow")
        violations += [
            f"{scope} references {label}; only {', '.join((*GATED_JOBS, *CANDIDATE_PUBLISH_JOBS))} may"
            for label in sorted(found)
        ]
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


def _run_strings(node: Any) -> Iterator[str]:
    """Yield every shell run string contained in a workflow node."""
    if isinstance(node, dict):
        for key, value in node.items():
            if key == "run" and isinstance(value, str):
                yield value
            yield from _run_strings(value)
    elif isinstance(node, list):
        for item in node:
            yield from _run_strings(item)


def _run_references(run: str) -> list[str]:
    """Find real publication references in a shell run string, ignoring quoted text."""
    labels = []
    command_texts: list[str] = []
    for line in _run_command_lines({"run": run}):
        for command, _ in _split_shell_commands(line):
            command_texts.append(command)
            tokens = _command_tokens(command)
            if not tokens:
                continue
            if tokens[0] == "npm" and len(tokens) > 1 and tokens[1] == "publish":
                labels.append("npm publish")
            if tokens[0] in ("python", "python3") and NPM_PUBLISH_SCRIPT in tokens:
                labels.append("the npm tarball publisher script")
            if re.search(r"\bdotnet[\s\\]+nuget[\s\\]+push\b", " ".join(tokens), re.IGNORECASE):
                labels.append("dotnet nuget push")

    expressions = EXPRESSION.findall(run)
    if any(WHOLE_SECRETS.search(expression) for expression in expressions):
        labels.append("the whole secrets context")
    if any(INDEXED_SECRETS.search(_blank_string_literals(expression)) for expression in expressions):
        labels.append("an indexed secrets access")
    for label, pattern in PUBLICATION_REFERENCES.items():
        if label in ("npm publish", "the npm tarball publisher script", "dotnet nuget push"):
            continue
        if any(pattern.search(expression) for expression in expressions):
            labels.append(label)
    return labels


def _step_run(step: dict) -> str:
    return " ".join(str(step.get("run") or "").split())


def _run_lines(step: dict) -> list[str]:
    """Whitespace-normalized non-empty lines of a step's run: body."""
    return [" ".join(line.split()) for line in str(step.get("run") or "").splitlines() if line.strip()]


def _run_command_lines(step: dict) -> list[str]:
    """Lines of a step's run with shell line continuations joined and comments removed."""
    raw = _run_lines(step)
    merged = []
    for line in raw:
        if merged and merged[-1].endswith("\\"):
            merged[-1] = merged[-1][:-1].rstrip() + " " + line
        else:
            merged.append(line)
    result = []
    for line in merged:
        comment = line.find("#")
        if comment >= 0:
            line = line[:comment].rstrip()
        if line:
            result.append(line)
    return result


def _split_shell_commands(line: str) -> list[tuple[str, str | None]]:
    """Split a shell line into commands separated by top-level ; && || |.

    Respects quoting so operators inside strings are not treated as separators.
    """
    try:
        lexer = shlex.shlex(line, posix=True, punctuation_chars=";&|")
        lexer.whitespace_split = True
        tokens = list(lexer)
    except ValueError:
        return []
    commands = []
    current: list[str] = []
    for token in tokens:
        if token in (";", "&&", "||", "|"):
            if current:
                commands.append((" ".join(current), token))
                current = []
        else:
            current.append(token)
    if current:
        commands.append((" ".join(current), None))
    return commands


def _command_tokens(command: str) -> list[str] | None:
    try:
        return shlex.split(command)
    except ValueError:
        return None


def _is_publisher_command(command: str) -> bool:
    tokens = _command_tokens(command)
    if tokens is None or len(tokens) < 2:
        return False
    return Path(tokens[0]).name in {"python", "python3"} and tokens[1] == NPM_PUBLISH_SCRIPT


def _is_npm_publish_command(command: str) -> bool:
    tokens = _command_tokens(command)
    if tokens is None or len(tokens) < 2:
        return False
    return tokens[0] == "npm" and tokens[1] == "publish"


def _publisher_command_line(command_lines: list[str]) -> str | None:
    """The first command line that actually invokes the npm tarball publisher, if any."""
    for line in command_lines:
        for command, _ in _split_shell_commands(line):
            if _is_publisher_command(command):
                return line
    return None


def _publisher_command(line: str) -> str | None:
    """The publisher command within a command line, if any."""
    for command, _ in _split_shell_commands(line):
        if _is_publisher_command(command):
            return command
    return None


def _parse_publisher_args(command: str) -> dict[str, list[str | None]]:
    """Map argument names to their values for a publisher invocation."""
    tokens = _command_tokens(command)
    if tokens is None:
        return {}
    args: dict[str, list[str | None]] = {}
    i = 2
    while i < len(tokens):
        token = tokens[i]
        if token.startswith("-"):
            if i + 1 < len(tokens) and not tokens[i + 1].startswith("-"):
                args.setdefault(token, []).append(tokens[i + 1])
                i += 2
            else:
                args.setdefault(token, []).append(None)
                i += 1
        else:
            i += 1
    return args


def _single_arg_value(args: dict[str, list[str | None]], name: str) -> str | None:
    """Single value for an option, or None if missing or duplicated."""
    values = args.get(name)
    if not values or len(values) != 1:
        return None
    return values[0]


def _has_flag(args: dict[str, list[str | None]], name: str) -> bool:
    """Whether an option appears exactly once as a boolean flag."""
    values = args.get(name)
    return values is not None and len(values) == 1 and values[0] is None


def _validate_publisher_args(
    args: dict[str, list[str | None]], *, package: str, tarball: str
) -> list[str]:
    """Validate the parsed arguments of a publisher invocation."""
    errors = []
    if _single_arg_value(args, "--package") != package:
        errors.append(f"must target {package}")
    if _single_arg_value(args, "--tarball") != tarball:
        errors.append("must pass the correct tarball")
    tag = _single_arg_value(args, "--tag")
    if tag is None or tag != "$tag":
        errors.append("must pass --tag $tag")
    registry = _single_arg_value(args, "--registry")
    if registry is None:
        errors.append("must pass exactly one --registry argument")
    elif registry != FEEDZ_NPM_REGISTRY:
        errors.append("must use the Feedz npm registry")
    if not _has_flag(args, "--verify-published"):
        errors.append("must pass --verify-published")
    return errors


def _step_publisher(step: dict) -> tuple[str, dict[str, list[str | None]]] | None:
    """The publisher command and its parsed arguments for a step, if any."""
    for line in _run_command_lines(step):
        command = _publisher_command(line)
        if command is not None:
            return command, _parse_publisher_args(command)
    return None


def _step_package(step: dict) -> str | None:
    """The --package value of the publisher invocation in a step, if any."""
    publisher = _step_publisher(step)
    if publisher is None:
        return None
    return _single_arg_value(publisher[1], "--package")


def _publisher_line_index(command_lines: list[str]) -> int | None:
    for i, line in enumerate(command_lines):
        if _publisher_command(line) is not None:
            return i
    return None


def _is_output_command(command: str) -> bool:
    """True for the exact echo command that writes wasm-published=true to GITHUB_OUTPUT."""
    tokens = _command_tokens(command)
    if tokens is None:
        return False
    return (
        len(tokens) >= 4
        and tokens[0] == "echo"
        and "wasm-published=true" in tokens
        and ">>" in tokens
        and "$GITHUB_OUTPUT" in tokens
    )


def _output_command_index(command_lines: list[str]) -> int | None:
    """Index of the command line that writes wasm-published=true, if any."""
    for i, line in enumerate(command_lines):
        for command, _ in _split_shell_commands(line):
            if _is_output_command(command):
                return i
    return None


def _publisher_error_suppressions(command_lines: list[str]) -> list[str]:
    """Detect constructs that let a failed publisher continue."""
    errors = []
    for line in command_lines:
        commands = _split_shell_commands(line)
        for j, (command, sep) in enumerate(commands):
            if not _is_publisher_command(command):
                continue
            if sep == "||" and j + 1 < len(commands) and commands[j + 1][0] in ("true", ":"):
                errors.append("publisher is followed by error-suppressing || true/:")
            if sep == ";" and j + 1 < len(commands):
                errors.append("publisher is chained with ; before the output command")
            if sep == "|" and j + 1 < len(commands):
                errors.append("publisher is piped into another command before the output")
    return errors


def _errexit_disable_index(command_lines: list[str]) -> int | None:
    """Index of a command that disables errexit, if any."""
    for i, line in enumerate(command_lines):
        for command, _ in _split_shell_commands(line):
            tokens = _command_tokens(command)
            if tokens is None:
                continue
            if len(tokens) >= 2 and tokens[0] == "set":
                if "+e" in tokens[1:]:
                    return i
                if "+o" in tokens[1:] and any(tok in {"errexit", "e"} for tok in tokens[1:]):
                    return i
    return None


def _shell_override_violation(step: dict) -> str | None:
    """Reject non-default shells that could bypass the publisher or error handling."""
    shell = step.get("shell")
    if shell is None:
        return None
    allowed = {"bash", "sh", "pwsh", "powershell", "cmd"}
    if isinstance(shell, str) and shell.split()[0] in allowed:
        return None
    return "must use a standard shell"


def _wasm_step_if_violation(step: dict) -> str | None:
    """The WASM publish step must run whenever the job runs.

    Only an explicit, plain success() condition (or no condition at all) is
    accepted; anything else could skip the step and let the React publish run.
    """
    value = step.get("if")
    if value is None:
        return None
    expression = if_expression(value)
    text = "".join((expression if expression is not None else str(value)).split()).lower()
    if text == "success()":
        return None
    return "must not be conditionally skipped"





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
    job_env = job.get("env") if isinstance(job.get("env"), dict) else {}
    if "EXCLUDE_IDS" in job_env:
        violations.append("publish_nuget must not set EXCLUDE_IDS at job level")
    if guard is not None:
        if _run_lines(steps[guard]) != list(NUGET_GUARD_LINES):
            violations.append("publish_nuget candidate staging run: must be exactly stage and the ID-set guard")
        if _guard_step_extra_keys(steps[guard]):
            violations.append(
                "publish_nuget candidate staging keys must be among name, env, run, id, if, continue-on-error"
            )
        env = steps[guard].get("env") if isinstance(steps[guard].get("env"), dict) else {}
        if " ".join(str(env.get("EXCLUDE_IDS") or "").split()) != GUARD_EXCLUDE_IDS:
            violations.append("publish_nuget candidate guard env EXCLUDE_IDS must be ${{ inputs.exclude_ids }}")
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
    wasm_publish, extra = _unique_step(
        steps,
        lambda step: NPM_PUBLISH_SCRIPT in _step_run(step) and f'--package "{WASM_PACKAGE}"' in _step_run(step),
        missing="publish_npm must publish @elsa-workflows/elsa-studio-wasm",
        duplicate="publish_npm must contain exactly one @elsa-workflows/elsa-studio-wasm publish step",
    )
    violations += extra
    react_publish, extra = _unique_step(
        steps,
        lambda step: NPM_PUBLISH_SCRIPT in _step_run(step) and f'--package "{REACT_PACKAGE}"' in _step_run(step),
        missing="publish_npm must publish @elsa-workflows/elsa-studio-wasm-react",
        duplicate="publish_npm must contain exactly one @elsa-workflows/elsa-studio-wasm-react publish step",
    )
    violations += extra

    # Reject any real npm publish invocation anywhere in the job.
    for step in steps:
        for line in _run_command_lines(step):
            for command, _ in _split_shell_commands(line):
                if _is_npm_publish_command(command):
                    violations.append("publish_npm must not invoke npm publish directly")

    if wasm_publish is not None:
        run = _step_run(steps[wasm_publish])
        wasm_command_lines = _run_command_lines(steps[wasm_publish])
        publisher = _step_publisher(steps[wasm_publish])
        publisher_index = _publisher_line_index(wasm_command_lines)
        output_index = _output_command_index(wasm_command_lines)
        set_e_index = next((i for i, line in enumerate(wasm_command_lines) if 'set -euo pipefail' in line), None)
        set_plus_e_index = _errexit_disable_index(wasm_command_lines)

        if publisher is None:
            violations.append("publish_npm WASM publish must invoke scripts/publish_npm_tarball.py")
        else:
            for error in _validate_publisher_args(
                publisher[1], package=WASM_PACKAGE, tarball='$RUNNER_TEMP/studio-npm-pair/$wasm'
            ):
                violations.append(f"publish_npm WASM publish {error}")
            for error in _publisher_error_suppressions(wasm_command_lines):
                violations.append(f"publish_npm WASM publish must not suppress errors ({error})")

        if "::error::Feedz npm key resolved empty" not in run:
            violations.append("publish_npm WASM publish must fail closed when the Feedz key is empty")
        if steps[wasm_publish].get("id") != "publish_wasm":
            violations.append("publish_npm WASM publish step must have id 'publish_wasm'")
        if 'echo "wasm-published=true" >> "$GITHUB_OUTPUT"' not in str(steps[wasm_publish].get("run") or ""):
            violations.append("publish_npm WASM publish step must set output 'wasm-published=true'")
        if output_index is None:
            violations.append("publish_npm WASM publish must execute wasm-published output command")
        elif publisher_index is not None and output_index <= publisher_index:
            violations.append("publish_npm WASM output must occur after successful publication")
        if set_e_index is None:
            violations.append("publish_npm WASM publish must set -euo pipefail before publishing")
        elif publisher_index is not None and set_e_index > publisher_index:
            violations.append("publish_npm WASM publish must set -euo pipefail before publishing")
        if set_plus_e_index is not None:
            if publisher_index is None or set_plus_e_index < publisher_index:
                violations.append("publish_npm WASM publish must not disable errexit before publishing")
            if output_index is None or set_plus_e_index < output_index:
                violations.append("publish_npm WASM publish must not disable errexit before writing wasm-published output")
        shell_violation = _shell_override_violation(steps[wasm_publish])
        if shell_violation:
            violations.append(f"publish_npm WASM publish step {shell_violation}")
        if_violation = _wasm_step_if_violation(steps[wasm_publish])
        if if_violation:
            violations.append(f"publish_npm WASM publish step {if_violation}")

    if react_publish is not None:
        run = _step_run(steps[react_publish])
        react_command_lines = _run_command_lines(steps[react_publish])
        publisher = _step_publisher(steps[react_publish])
        publisher_index = _publisher_line_index(react_command_lines)
        set_e_index = next((i for i, line in enumerate(react_command_lines) if 'set -euo pipefail' in line), None)
        set_plus_e_index = _errexit_disable_index(react_command_lines)

        if publisher is None:
            violations.append("publish_npm React publish must invoke scripts/publish_npm_tarball.py")
        else:
            for error in _validate_publisher_args(
                publisher[1], package=REACT_PACKAGE, tarball='$RUNNER_TEMP/studio-npm-pair/$react'
            ):
                violations.append(f"publish_npm React publish {error}")

        if "::error::Feedz npm key resolved empty" not in run:
            violations.append("publish_npm React publish must fail closed when the Feedz key is empty")
        if if_expression(steps[react_publish].get("if")) != "steps.publish_wasm.outputs.wasm-published == 'true'":
            violations.append("publish_npm React publish must run only when WASM is confirmed")
        if set_e_index is None:
            violations.append("publish_npm React publish must set -euo pipefail before publishing")
        elif publisher_index is not None and set_e_index > publisher_index:
            violations.append("publish_npm React publish must set -euo pipefail before publishing")
        if set_plus_e_index is not None and (publisher_index is None or set_plus_e_index < publisher_index):
            violations.append("publish_npm React publish must not disable errexit before publishing")
        shell_violation = _shell_override_violation(steps[react_publish])
        if shell_violation:
            violations.append(f"publish_npm React publish step {shell_violation}")

    publisher_step_count = sum(1 for step in steps if _step_publisher(step) is not None)
    if publisher_step_count != 2:
        violations.append("publish_npm must contain exactly two tarball publish steps")
    if any(step.get("name") == "npm publish to npmjs.com" or re.search(r"registry\.npmjs\.(org|com)", _step_run(step)) for step in steps):
        violations.append("publish_npm must not publish to npmjs.com until trusted publishing is confirmed")
    if None not in (closed, download, verify, wasm_publish, react_publish) and not closed < download < verify < wasm_publish < react_publish:
        violations.append("publish_npm must fail closed, download and verify the pair, publish WASM, then publish React")
    return violations


def promotion_source_violations(source: str) -> list[str]:
    return [message for needle, message in FORBIDDEN_PACK_ROUTE if needle in source]


CANDIDATE_VALIDATE_JOB = "validate_candidate_pin"
CANDIDATE_RETRIEVE_JOB = "retrieve_candidate"
CANDIDATE_FEEDZ_JOB = "publish_candidate_feedz"
CANDIDATE_NUGET_JOB = "publish_candidate_nuget"
CANDIDATE_PUBLISH_JOBS = (CANDIDATE_FEEDZ_JOB, CANDIDATE_NUGET_JOB)
CANDIDATE_JOBS = (CANDIDATE_VALIDATE_JOB, CANDIDATE_RETRIEVE_JOB, *CANDIDATE_PUBLISH_JOBS)
CANDIDATE_VALIDATE_IF = "github.event_name == 'workflow_dispatch' && inputs.promote_candidate_version != ''"
VERIFIER = "scripts/verify_consolidated_candidate.py"
BLANK_TOKENS = {"GITHUB_TOKEN": "", "GH_TOKEN": "", "ACTIONS_READ_TOKEN": ""}
CONTEXT_LINES = (
    "set -euo pipefail",
    "test \"$GITHUB_REPOSITORY\" = 'elsa-workflows/elsa-core'",
    "test \"$GITHUB_REF\" = 'refs/heads/main'",
    "test \"$GITHUB_REF_TYPE\" = 'branch'",
    "test \"$GITHUB_RUN_ATTEMPT\" = '1'",
)
# Each publisher runs for its own target (or all) and never depends on a version.
CANDIDATE_PUBLISH_IF = {
    CANDIDATE_FEEDZ_JOB: "inputs.promote_target == 'all' || inputs.promote_target == 'feedz'",
    # nuget.org follows Feedz on 'all' (a Feedz failure blocks it) and runs alone on 'nuget'.
    CANDIDATE_NUGET_JOB: ("!cancelled() && needs.validate_candidate_pin.result == 'success'"
                          " && needs.retrieve_candidate.result == 'success'"
                          " && (inputs.promote_target == 'all' || inputs.promote_target == 'nuget')"
                          " && (needs.publish_candidate_feedz.result == 'success'"
                          " || (inputs.promote_target == 'nuget' && needs.publish_candidate_feedz.result == 'skipped'))"),
}
CANDIDATE_PUBLISH_NEEDS = {
    CANDIDATE_FEEDZ_JOB: {CANDIDATE_RETRIEVE_JOB},
    CANDIDATE_NUGET_JOB: {CANDIDATE_VALIDATE_JOB, CANDIDATE_RETRIEVE_JOB, CANDIDATE_FEEDZ_JOB},
}
CANDIDATE_TARGETS = ["all", "feedz", "nuget"]
CANDIDATE_ENVIRONMENTS = {CANDIDATE_FEEDZ_JOB: "feedz-publish", CANDIDATE_NUGET_JOB: "nuget-org"}
CANDIDATE_FEED = {CANDIDATE_FEEDZ_JOB: "feedz", CANDIDATE_NUGET_JOB: "nuget"}
CANDIDATE_GUARD_LINES = {
    job: ("set -euo pipefail", GUARD_CANDIDATE,
          f'python3 {VERIFIER} count --version "$VERSION" --feed {feed} --dir "$PACKAGES_DIR"')
    for job, feed in CANDIDATE_FEED.items()
}
CANDIDATE_MARKERS = ("skip_id_set", "skip_guard", "bypass_id_set", "bypass_guard", "expected_ids")
CANDIDATE_NO_TAGGING = ("git tag", "git push", "refs/tags", "gh release", "git/refs")
PINNED_ACTION = re.compile(r"[^@]+@[0-9a-f]{40}")
CANDIDATE_SECRET = "secrets.ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY"


def candidate_promotion_violations(source: str) -> list[str]:
    """Candidate promotion installs pinned bytes to Feedz and nuget.org: same steps for every version.

    Checks the four candidate jobs of packages.yml (source text, so mutations can be tested): the pin is
    validated first, retrieval has no environment, each publisher re-verifies tokenless and runs the ID-set
    guard and the pinned count before any key or login, and nothing is conditional on a version.
    """
    violations: list[str] = []
    workflow = yaml.safe_load(source)
    jobs = workflow.get("jobs") if isinstance(workflow, dict) else None
    if not isinstance(jobs, dict):
        return ["candidate promotion: the workflow has no jobs mapping"]
    prefix = "candidate promotion"
    for name in CANDIDATE_JOBS:
        if not isinstance(jobs.get(name), dict):
            violations.append(f"{prefix}: job {name!r} is missing")
    if violations:
        return violations
    section = {name: jobs[name] for name in CANDIDATE_JOBS}
    text = " ".join(strings(section)).lower()
    for marker in CANDIDATE_MARKERS:
        if marker in text:
            violations.append(f"{prefix}: must not offer a skip, bypass, or ID-list override")
    for needle in CANDIDATE_NO_TAGGING:
        if needle in text:
            violations.append(f"{prefix}: must not create or push a tag or release ({needle})")
    if _has_defaults_run_shell(workflow) or any(_has_defaults_run_shell(job) for job in section.values()):
        violations.append(f"{prefix}: must not set defaults.run.shell")
    inputs = (triggers(workflow).get("workflow_dispatch") or {}).get("inputs") or {}
    spec = inputs.get("promote_candidate_version")
    if not isinstance(spec, dict) or spec.get("type") != "string" or spec.get("default") != "":
        violations.append(f"{prefix}: workflow_dispatch input 'promote_candidate_version' must be a string defaulting to ''")
    target = inputs.get("promote_target")
    if (not isinstance(target, dict) or target.get("type") != "choice" or target.get("default") != "all"
            or target.get("options") != CANDIDATE_TARGETS):
        violations.append(f"{prefix}: workflow_dispatch input 'promote_target' must be a choice of {CANDIDATE_TARGETS} defaulting to 'all'")
    for name, job in section.items():
        steps = [step for step in job.get("steps") or [] if isinstance(step, dict)]
        if any(node.get("continue-on-error") not in (None, False) for node in (job, *steps)):
            violations.append(f"{prefix}: {name} must not continue on error")
        if any("if" in step for step in steps):
            violations.append(f"{prefix}: {name} steps must not have an if:; every version runs the same steps")
        if re.search(r"\b[0-9]+\.[0-9]+\.[0-9]+\b", " ".join(strings(job))):
            violations.append(f"{prefix}: {name} must not name a version; versions live in the pin file")
        for step in steps:
            uses = str(step.get("uses") or "")
            if uses and not uses.startswith("./") and not PINNED_ACTION.fullmatch(uses):
                violations.append(f"{prefix}: {name} action {uses!r} must be pinned to a full commit SHA")
        conditions = [if_expression(node["if"]) or "" for node in (job, *steps) if "if" in node]
        expressions = [e for t in strings(job) for e in EXPRESSION.findall(t)]
        if name == CANDIDATE_NUGET_JOB:
            # Its pinned if: (checked exactly below) is the one place cancelled() is allowed.
            pinned = CANDIDATE_PUBLISH_IF[name]
            conditions = [c for c in conditions if c != pinned]
            expressions = [e for e in expressions if e.strip() != pinned]
        if any(STATUS_FUNCTION.search(e) for e in (*expressions, *conditions)):
            violations.append(f"{prefix}: {name} must not call always(), failure() or cancelled()")
    validate = section[CANDIDATE_VALIDATE_JOB]
    if SELECTION_JOB not in needs(validate):
        violations.append(f"{prefix}: {CANDIDATE_VALIDATE_JOB} must need {SELECTION_JOB}")
    if if_expression(validate.get("if")) != CANDIDATE_VALIDATE_IF:
        violations.append(f"{prefix}: {CANDIDATE_VALIDATE_JOB} if: must be exactly {CANDIDATE_VALIDATE_IF}")
    first_run = next((step for step in validate.get("steps") or [] if isinstance(step, dict) and "run" in step), None)
    if first_run is None or f"{VERIFIER} validate-pin" not in _step_run(first_run):
        violations.append(f"{prefix}: the first run step of {CANDIDATE_VALIDATE_JOB} must validate the pin entry")
    if "if" in section[CANDIDATE_RETRIEVE_JOB]:
        violations.append(f"{prefix}: {CANDIDATE_RETRIEVE_JOB} must not have its own if:; it follows {CANDIDATE_VALIDATE_JOB} through needs")
    for name in CANDIDATE_PUBLISH_JOBS:
        if if_expression(section[name].get("if")) != CANDIDATE_PUBLISH_IF[name]:
            violations.append(f"{prefix}: {name} if: must be exactly {CANDIDATE_PUBLISH_IF[name]} (keyed on the promote target and job results, never a version)")
    retrieve = section[CANDIDATE_RETRIEVE_JOB]
    if needs(retrieve) != {CANDIDATE_VALIDATE_JOB}:
        violations.append(f"{prefix}: {CANDIDATE_RETRIEVE_JOB} must need only {CANDIDATE_VALIDATE_JOB}")
    scan = " ".join(strings(retrieve))
    if "environment" in retrieve or "secrets" in scan.lower() or grants_id_token(retrieve.get("permissions")):
        violations.append(f"{prefix}: {CANDIDATE_RETRIEVE_JOB} must have no environment, secret or id-token")
    if not any(f"{VERIFIER} retrieve" in _step_run(step) for step in retrieve.get("steps") or [] if isinstance(step, dict)):
        violations.append(f"{prefix}: {CANDIDATE_RETRIEVE_JOB} must run the verifier's retrieve")
    for name, job in section.items():
        if name in (CANDIDATE_VALIDATE_JOB,):
            continue
        steps = [step for step in job.get("steps") or [] if isinstance(step, dict)]
        first = next((step for step in steps if "run" in step), None)
        if first is None or _run_lines(first) != list(CONTEXT_LINES):
            violations.append(f"{prefix}: the first run step of {name} must require the repository, refs/heads/main and run attempt 1")
    for name in CANDIDATE_PUBLISH_JOBS:
        job = section[name]
        steps = [step for step in job.get("steps") or [] if isinstance(step, dict)]
        feed = CANDIDATE_FEED[name]
        if needs(job) != CANDIDATE_PUBLISH_NEEDS[name]:
            violations.append(f"{prefix}: {name} must need exactly {sorted(CANDIDATE_PUBLISH_NEEDS[name])}")
        if job.get("environment") != CANDIDATE_ENVIRONMENTS[name]:
            violations.append(f"{prefix}: {name} must use environment {CANDIDATE_ENVIRONMENTS[name]}")
        if grants_id_token(job.get("permissions")) != (name == CANDIDATE_NUGET_JOB):
            violations.append(f"{prefix}: only the nuget publisher may request id-token: write")
        verify, extra = _unique_step(steps, lambda step: f"{VERIFIER} verify" in _step_run(step),
                                     missing=f"{prefix}: {name} must re-verify the pin before publishing",
                                     duplicate=f"{prefix}: {name} must contain exactly one verify step")
        violations += extra
        guard, extra = _unique_step(steps, lambda step: GUARD_CANDIDATE in _step_run(step),
                                    missing=f"{prefix}: {name} must run the ID-set guard before publishing",
                                    duplicate=f"{prefix}: {name} must contain exactly one ID-set guard step")
        violations += extra
        publish = next((i for i, step in enumerate(steps)
                        if "push-nuget-packages" in str(step.get("uses") or "")), None)
        secret = next((i for i, step in enumerate(steps)
                       if CANDIDATE_SECRET in str(step) or "nuget/login" in str(step.get("uses") or "").lower()), None)
        if publish is None or secret is None:
            violations.append(f"{prefix}: {name} must publish with the push-nuget-packages action after its key or login")
        if None not in (verify, guard, secret, publish) and not verify < guard < secret <= publish:
            violations.append(f"{prefix}: {name} must verify, guard and count before the key or login, then publish")
        if verify is not None and (steps[verify].get("env") or {}) != {**BLANK_TOKENS, "VERSION": "${{ inputs.promote_candidate_version }}"}:
            violations.append(f"{prefix}: {name} verify step must blank GITHUB_TOKEN, GH_TOKEN and ACTIONS_READ_TOKEN")
        if guard is not None:
            step = steps[guard]
            if _run_lines(step) != list(CANDIDATE_GUARD_LINES[name]):
                violations.append(f"{prefix}: {name} guard run: must be exactly set -euo pipefail, the ID-set guard and the pinned count")
            if _guard_step_extra_keys(step):
                violations.append(f"{prefix}: {name} guard step keys must be among name, env, run, id, if, continue-on-error")
            env = step.get("env") if isinstance(step.get("env"), dict) else {}
            expected_exclude = "${{ steps.pin.outputs.exclude_ids }}" if feed == "nuget" else None
            if env.get("EXCLUDE_IDS") != expected_exclude:
                violations.append(
                    f"{prefix}: {name} guard EXCLUDE_IDS must be the pinned exclusions"
                    if feed == "nuget" else f"{prefix}: {name} must not set EXCLUDE_IDS")
        if feed == "feedz" and "EXCLUDE_IDS" in " ".join(strings(job)):
            violations.append(f"{prefix}: {name} must not set EXCLUDE_IDS")
        if CANDIDATE_SECRET in " ".join(strings(job)) and name != CANDIDATE_FEEDZ_JOB:
            violations.append(f"{prefix}: only the Feedz publisher may read the Feedz key")
    return violations


def workflow_violations(workflow: Any) -> list[str]:
    if not isinstance(workflow, dict) or not isinstance(workflow.get("jobs"), dict):
        return ["the workflow has no jobs mapping"]
    jobs = workflow["jobs"]
    defaults = ["the workflow must not set defaults.run.shell"] if _has_defaults_run_shell(workflow) else []
    workflow_env = workflow.get("env") if isinstance(workflow.get("env"), dict) else {}
    exclude_ids = (
        ["packages.yml must not set EXCLUDE_IDS at workflow level"]
        if "EXCLUDE_IDS" in workflow_env
        else []
    )
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
        *exclude_ids,
    ]


def source_violations(source: str) -> list[str]:
    return [
        *workflow_violations(yaml.safe_load(source)),
        *promotion_source_violations(source),
        *candidate_promotion_violations(source),
    ]


def main() -> int:
    violations = [
        *(f"{ID_SET.relative_to(ROOT)}: {violation}" for violation in id_set_count_violations()),
        *(f"{WORKFLOW.relative_to(ROOT)}: {violation}" for violation in source_violations(WORKFLOW.read_text(encoding="utf-8"))),
    ]
    for violation in violations:
        print(violation, file=sys.stderr)
    if violations:
        return 1
    print("Package publication gates hold: only the default-off, validated dispatch jobs can publish or deploy.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

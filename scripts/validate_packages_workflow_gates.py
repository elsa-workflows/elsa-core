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

SELECTION_JOB = "validate_publication_selection"
# Each gated job is switched on by the workflow_dispatch input of the same name.
GATED_JOBS = ("publish_preview_feedz", "publish_nuget", "deploy_coverage")
MANUAL_DISPATCH = "github.event_name == 'workflow_dispatch'"
# Top-level conjuncts a gated job's if: needs beyond the dispatch and its own input.
REQUIRED_REF_CONDITIONS = {
    "publish_nuget": (
        "startsWith(github.ref, 'refs/tags/')",
        # Defense in depth: 3.10.x stays off NuGet.org even if the selection job were bypassed.
        "!startsWith(github.ref_name, '3.10.')",
    ),
    "deploy_coverage": (" || ".join(f"github.ref == '{ref}'" for ref in COVERAGE_REFS),),
}
SELECTION_COMMAND = "python3 scripts/validate_packages_publication_selection.py"
SELECTION_ENV = {
    "REF": "${{ github.ref }}",
    "PUBLISH_PREVIEW_FEEDZ": "${{ inputs.publish_preview_feedz }}",
    "PUBLISH_NUGET": "${{ inputs.publish_nuget }}",
    "DEPLOY_COVERAGE": "${{ inputs.deploy_coverage }}",
}
# Probed against the script the selection job runs, not a copy of its rules.
REQUIRED_REJECTIONS = (
    ("NuGet.org publication of a 3.10.x tag", "refs/tags/3.10.0", {"nuget": True}),
    ("a v-prefixed tag", "refs/tags/v3.10.0", {}),
    ("a non-release tag", "refs/tags/test-1", {}),
)
# Capabilities only a gated job may reference. Expression contexts and action names are case-insensitive.
PUBLICATION_REFERENCES = {
    "secrets.FEEDZ*": re.compile(r"\bsecrets\s*(?:\.|\[\s*')\s*feedz", re.IGNORECASE),
    "secrets.NUGET*": re.compile(r"\bsecrets\s*(?:\.|\[\s*')\s*nuget", re.IGNORECASE),
    "NuGet/login": re.compile(r"\bnuget/login\b", re.IGNORECASE),
    "actions/deploy-pages": re.compile(r"\bactions/deploy-pages\b", re.IGNORECASE),
    "dotnet nuget push": re.compile(r"\bdotnet[\s\\]+nuget[\s\\]+push\b", re.IGNORECASE),
    "the push-nuget-packages action": re.compile(r"push-nuget-packages", re.IGNORECASE),
}
EXPRESSION = re.compile(r"\$\{\{(.*?)\}\}", re.DOTALL)
# Inside ${{ }}: the secrets context as a whole, e.g. toJSON(secrets), which carries every feed key.
WHOLE_SECRETS = re.compile(r"\bsecrets\b(?!\s*[.\[])", re.IGNORECASE)
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
        if any(WHOLE_SECRETS.search(expression) for text in texts for expression in EXPRESSION.findall(text)):
            found.append("the whole secrets context")
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


def workflow_violations(workflow: Any) -> list[str]:
    if not isinstance(workflow, dict) or not isinstance(workflow.get("jobs"), dict):
        return ["the workflow has no jobs mapping"]
    jobs = workflow["jobs"]
    return [
        *trigger_violations(workflow),
        *capability_violations(workflow),
        *gated_job_violations(jobs),
        *build_gate_violations(jobs),
        *selection_violations(jobs),
        *pages_upload_violations(jobs),
    ]


def source_violations(source: str) -> list[str]:
    return workflow_violations(yaml.safe_load(source))


def main() -> int:
    violations = source_violations(WORKFLOW.read_text(encoding="utf-8"))
    for violation in violations:
        print(f"{WORKFLOW.relative_to(ROOT)}: {violation}", file=sys.stderr)
    if violations:
        return 1
    print("Package publication gates hold: only the three default-off, validated dispatch jobs can publish or deploy.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

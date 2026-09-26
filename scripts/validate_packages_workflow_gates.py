#!/usr/bin/env python3
"""Check that package publishers and coverage deployment require per-run opt-in."""

from __future__ import annotations

import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/packages.yml"
SOURCE = WORKFLOW.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def input_property(name: str, property_name: str) -> str:
    match = re.search(
        rf"^      {re.escape(name)}:\n(?P<body>(?:^        .*\n)*)",
        SOURCE,
        re.MULTILINE,
    )
    require(match is not None, f"workflow_dispatch input {name!r} is missing")
    value = re.search(
        rf"^        {re.escape(property_name)}: (\w+)\s*$",
        match.group("body"),
        re.MULTILINE,
    )
    require(value is not None, f"input {name!r} has no explicit {property_name}")
    return value.group(1).lower()


def job_condition(name: str) -> str:
    match = re.search(
        rf"^  {re.escape(name)}:\n(?P<body>.*?)(?=^  [\w-]+:\n|\Z)",
        SOURCE,
        re.MULTILINE | re.DOTALL,
    )
    require(match is not None, f"job {name!r} is missing")
    condition = re.search(r"^    if: (.+)$", match.group("body"), re.MULTILINE)
    require(condition is not None, f"job {name!r} has no explicit condition")
    expression = condition.group(1).strip()
    if expression.startswith("${{") and expression.endswith("}}"):
        expression = expression[3:-2].strip()
    return expression


EXPECTED_CONDITIONS = {
    "publish_preview_feedz": "github.event_name == 'workflow_dispatch' && inputs.publish_preview_feedz && startsWith(github.ref, 'refs/heads/')",
    "publish_nuget": "github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/')",
    "deploy_coverage": "github.event_name == 'workflow_dispatch' && inputs.deploy_coverage && (github.ref == 'refs/heads/main' || github.ref == 'refs/heads/develop/3.6.1' || github.ref == 'refs/heads/release/3.6.1')",
}


def enabled(job: str, *, event: str, ref: str, inputs: dict[str, bool]) -> bool:
    opted_in = inputs.get(job, False)
    if event != "workflow_dispatch" or not opted_in:
        return False
    if job == "publish_preview_feedz":
        return ref.startswith("refs/heads/")
    if job == "publish_nuget":
        return ref.startswith("refs/tags/")
    if job == "deploy_coverage":
        return ref in {
            "refs/heads/main",
            "refs/heads/develop/3.6.1",
            "refs/heads/release/3.6.1",
        }
    raise AssertionError(f"no gate evaluator for {job}")


def main() -> None:
    for name in ("publish_preview_feedz", "publish_nuget", "deploy_coverage"):
        require(input_property(name, "default") == "false", f"{name} must default to false")
        require(input_property(name, "type") == "boolean", f"{name} must be Boolean")

    require(
        job_condition("validate_publication_selection") == "github.event_name == 'workflow_dispatch'",
        "publication selection validation must run on manual dispatch",
    )
    require(
        SOURCE.count("needs: [build, validate_publication_selection]") == 2,
        "both package publishers must depend on selection validation",
    )
    require(
        "needs: [coverage_report, validate_publication_selection]" in SOURCE,
        "coverage deployment must depend on selection validation",
    )

    cases = (
        (
            "normal main push",
            "push",
            "refs/heads/main",
            {},
            {"publish_preview_feedz": False, "publish_nuget": False, "deploy_coverage": False},
        ),
        (
            "unselected manual dispatch",
            "workflow_dispatch",
            "refs/heads/main",
            {},
            {"publish_preview_feedz": False, "publish_nuget": False, "deploy_coverage": False},
        ),
        (
            "automatic release event",
            "release",
            "refs/tags/3.10.0",
            {},
            {"publish_preview_feedz": False, "publish_nuget": False, "deploy_coverage": False},
        ),
        (
            "Feedz opt-in",
            "workflow_dispatch",
            "refs/heads/main",
            {"publish_preview_feedz": True},
            {"publish_preview_feedz": True},
        ),
        (
            "NuGet opt-in",
            "workflow_dispatch",
            "refs/tags/3.10.0",
            {"publish_nuget": True},
            {"publish_nuget": True},
        ),
        (
            "coverage opt-in",
            "workflow_dispatch",
            "refs/heads/main",
            {"deploy_coverage": True},
            {"deploy_coverage": True},
        ),
        (
            "Feedz opt-in on tag",
            "workflow_dispatch",
            "refs/tags/3.10.0",
            {"publish_preview_feedz": True},
            {},
        ),
        (
            "NuGet opt-in on branch",
            "workflow_dispatch",
            "refs/heads/main",
            {"publish_nuget": True},
            {},
        ),
        (
            "coverage opt-in on ineligible branch",
            "workflow_dispatch",
            "refs/heads/feature/coverage",
            {"deploy_coverage": True},
            {},
        ),
    )
    conditions = {job: job_condition(job) for job in EXPECTED_CONDITIONS}
    for job, expected_expression in EXPECTED_CONDITIONS.items():
        require(
            conditions[job] == expected_expression,
            f"{job} condition changed; review the expected opt-in gate",
        )

    for case_name, event, ref, inputs, expected in cases:
        for job in conditions:
            actual = enabled(job, event=event, ref=ref, inputs=inputs)
            require(
                actual == expected.get(job, False),
                f"{case_name}: {job} expected {expected.get(job, False)}, got {actual}",
            )

    require(
        "github.event_name != 'pull_request'" in SOURCE,
        "package build must remain available for push and workflow_dispatch events",
    )
    upload_condition = re.search(
        r"^        if: \$\{\{ (.+) \}\}$",
        SOURCE[SOURCE.index("      - name: Upload artifact"):],
        re.MULTILINE,
    )
    require(upload_condition is not None, "package artifact upload condition is missing")
    expected_upload_condition = "github.event_name == 'release' || github.event_name == 'push' || (github.event_name == 'workflow_dispatch' && (inputs.publish_preview_feedz || inputs.publish_nuget))"
    require(
        upload_condition.group(1) == expected_upload_condition,
        "package artifact upload condition changed; review its opt-in behavior",
    )
    def uploads_package_artifact(event: str, inputs: dict[str, bool]) -> bool:
        return event in {"release", "push"} or (
            event == "workflow_dispatch"
            and (inputs.get("publish_preview_feedz", False) or inputs.get("publish_nuget", False))
        )

    require(
        uploads_package_artifact("push", {}),
        "normal push must still upload the built package artifact",
    )
    require(
        not uploads_package_artifact("workflow_dispatch", {}),
        "an unselected manual dispatch must not create a package artifact",
    )
    for event, inputs in (
        ("workflow_dispatch", {"publish_preview_feedz": True}),
        ("workflow_dispatch", {"publish_nuget": True}),
    ):
        require(
            uploads_package_artifact(event, inputs),
            "an explicitly approved manual publication run must upload its packages",
        )

    print("Package publisher and coverage deployment gates passed (default-off and opt-in cases).")


if __name__ == "__main__":
    main()

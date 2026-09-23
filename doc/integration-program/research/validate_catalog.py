#!/usr/bin/env python3
"""Validate the integration catalog against its JSON Schema and evidence graph."""
import json
import sys
from datetime import date
from pathlib import Path

HERE = Path(__file__).resolve().parent
SCHEMA_PATH = HERE / "schema.json"
CATALOG_PATH = HERE / "candidate-catalog.json"


def fail(message):
    raise ValueError(message)


def main():
    schema = json.loads(SCHEMA_PATH.read_text())
    catalog = json.loads(CATALOG_PATH.read_text())
    try:
        from jsonschema import Draft202012Validator, FormatChecker
    except ImportError as error:
        fail("Install the Python 'jsonschema' package to validate Draft 2020-12 schema: " + str(error))
    Draft202012Validator.check_schema(schema)
    errors = sorted(Draft202012Validator(schema, format_checker=FormatChecker()).iter_errors(catalog), key=lambda e: list(e.absolute_path))
    if errors:
        for error in errors:
            location = ".".join(str(part) for part in error.absolute_path) or "<root>"
            print(f"{location}: {error.message}", file=sys.stderr)
        fail(f"JSON Schema reported {len(errors)} error(s)")

    sources = catalog["sources"]
    source_by_id = {item["id"]: item for item in sources}
    if len(source_by_id) != len(sources):
        fail("Source IDs must be unique")
    providers = catalog["providers"]
    provider_by_id = {item["stableId"]: item for item in providers}
    if len(provider_by_id) != len(providers):
        fail("Provider IDs must be unique")
    if len(providers) != catalog["metadata"]["candidateCount"]:
        fail("candidateCount does not match provider record count")
    deep = [p for p in providers if p["assessmentLevel"] == "deep"]
    if len(deep) != catalog["metadata"]["deepCount"]:
        fail("deepCount does not match deep record count")
    if not 50 <= len(providers) <= 100 or not 15 <= len(deep) <= 20:
        fail("Expected 50-100 candidates and 15-20 deep provider assessments")

    assessment_keys = [
        "existingElsaStatus", "authenticationTypesAndScopes", "oauthAppVerification", "apiDocumentation",
        "webhookDocumentation", "pollingRenewalRequirements", "paginationAndRateLimits", "fileSizeConstraints",
        "paidTierApiRestrictions", "sandboxTestAvailability", "sdkLicenseConsiderations", "dataResidency",
        "maintenanceBurden", "ownershipSponsorship", "sharedCloudEligibility", "elsaCompatibility"
    ]
    def check_assessment(value, location):
        status = value["status"]
        ids = value["sourceIds"]
        if len(ids) != len(set(ids)):
            fail(f"{location}: duplicate evidence source IDs")
        missing = sorted(set(ids) - source_by_id.keys())
        if missing:
            fail(f"{location}: unresolved source IDs {missing}")
        if status in {"verified", "inferred", "mixed"}:
            if not ids or not value["checkedDate"]:
                fail(f"{location}: {status} claim needs source IDs and checkedDate")
            try:
                date.fromisoformat(value["checkedDate"])
            except (TypeError, ValueError):
                fail(f"{location}: checkedDate is not an ISO date")
        if status == "unknown" and not value["note"].strip():
            fail(f"{location}: unknown must say why it is unknown")

    operation_rows = [op for provider in providers for op in provider["proposedOperations"]]
    operation_ids = [op["id"] for op in operation_rows]
    if len(set(operation_ids)) != len(operation_ids):
        fail("Operation IDs must be unique across all providers")
    workflow_ids = [workflow["id"] for workflow in catalog["workflowHypotheses"]]
    if len(set(workflow_ids)) != len(workflow_ids):
        fail("Workflow IDs must be unique")

    for provider in providers:
        pid = provider["stableId"]
        for source_id in provider["evidence"]:
            if source_id not in source_by_id:
                fail(f"{pid}: unresolved evidence source {source_id}")
        for key in assessment_keys:
            check_assessment(provider[key], f"{pid}.{key}")
        for operation in provider["proposedOperations"]:
            op_id = operation["id"]
            for source_id in operation["sourceIds"]:
                if source_id not in source_by_id:
                    fail(f"{op_id}: unresolved source {source_id}")
            check_assessment(operation["authScope"], f"{op_id}.authScope")
            kinds = {source_by_id[s]["kind"] for s in operation["sourceIds"]}
            if operation["evidenceStatus"] == "verified_official_docs" and "official_provider_docs" not in kinds:
                fail(f"{op_id}: official-doc claim has no official provider documentation source")
            if operation["evidenceStatus"] == "verified_existing_source" and "repository_source" not in kinds:
                fail(f"{op_id}: existing-source claim has no repository source")
        if provider["demandScore"] is not None and not provider["demandEvidence"]:
            fail(f"{pid}: demand score requires evidence")

    # Each deep provider's evidence array is a source index, not a substitute for claim-level citations.
    operations = {op["id"]: p["stableId"] for p in providers for op in p["proposedOperations"]}
    for workflow in catalog["workflowHypotheses"]:
        for stage in workflow["stages"]:
            provider_id = stage["providerId"]
            operation_id = stage["operationId"]
            if provider_id == "elsa.internal":
                if not operation_id.startswith("elsa.internal."):
                    fail(f"{workflow['id']}: internal workflow operation must use the elsa.internal namespace")
                continue
            if provider_id not in provider_by_id:
                fail(f"{workflow['id']}: unknown provider {provider_id}")
            if operation_id not in operations:
                fail(f"{workflow['id']}: unknown operation {operation_id}")
            if operations[operation_id] != provider_id:
                fail(f"{workflow['id']}: operation {operation_id} belongs to a different provider")

    rubric = catalog["rubric"]
    if abs(sum(item["weight"] for item in rubric) - 1.0) > 1e-9:
        fail("Rubric weights must sum to 1")
    if len({item["dimension"] for item in rubric}) != len(rubric):
        fail("Rubric dimensions must be unique")
    dimension_weights = {item["dimension"]: item["weight"] for item in rubric}
    metric_dimensions = {
        "workflowCoverage": "complete-workflow coverage",
        "feasibility": "feasibility",
        "infrastructureReuse": "shared-infrastructure reuse",
        "sustainability": "sustainability",
    }
    if "evidenced demand" not in dimension_weights or set(metric_dimensions.values()) - dimension_weights.keys():
        fail("Rubric dimensions do not match the supported score metrics")
    demand_weight = dimension_weights["evidenced demand"]
    for provider in providers:
        scores = provider["scores"]
        if scores is None:
            continue
        known = sum(dimension_weights[dimension] * scores[metric] for metric, dimension in metric_dimensions.items())
        sens = scores["demandSensitivity"]
        expected_values = {
            "demand0": known,
            "demand2_5": known + demand_weight * 2.5,
            "demand5": known + demand_weight * 5,
        }
        for scenario, expected in expected_values.items():
            if abs(sens[scenario] - expected) > 0.0011:
                fail(f"{provider['stableId']}: {scenario} is inconsistent with rubric weights")
        expected_range = [known, known + demand_weight * 5]
        for left, right in zip(expected_range, sens["weightedRange"]):
            if abs(left - right) > 0.0011:
                fail(f"{provider['stableId']}: weightedRange is inconsistent with rubric weights")
    for source in sources:
        if not source["url"].startswith("https://"):
            fail(f"{source['id']}: sources must use HTTPS")
    cohorts = catalog["rankedRecommendation"]["cohorts"]
    ranks = [cohort["rank"] for cohort in cohorts]
    if ranks != list(range(1, len(cohorts) + 1)):
        fail("Recommendation cohort ranks must be contiguous and ordered")
    ranked_ids = [pid for cohort in cohorts for pid in cohort["providerIds"]]
    if len(set(ranked_ids)) != len(ranked_ids):
        fail("A provider may appear in only one recommendation cohort")
    if set(ranked_ids) != {provider["stableId"] for provider in deep}:
        fail("Recommendation cohorts must rank every deep provider exactly once")
    if catalog["metadata"]["checkedDate"] != max(p["checkedDate"] for p in providers):
        fail("Catalog checked date must not precede provider checked dates")
    print(f"PASS: Draft 2020-12 schema; {len(providers)} unique candidates; {len(deep)} sourced deep assessments; {len(operations)} operation rows; {len(catalog['workflowHypotheses'])} workflows; {len(sources)} resolvable sources; rubric and score sensitivity consistent")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, KeyError, TypeError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        sys.exit(1)

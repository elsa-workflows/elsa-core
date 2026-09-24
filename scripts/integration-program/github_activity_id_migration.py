#!/usr/bin/env python3
"""Fail-closed offline migration for legacy Elsa GitHub activity IDs."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import os
import re
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Any


LEGACY_TYPES = {
    "Elsa.GitHub.Comments.DeleteComment": ("commentId", "Int32"),
    "Elsa.GitHub.Comments.GetComment": ("commentId", "Int32"),
    "Elsa.GitHub.Comments.UpdateComment": ("commentId", "Int32"),
    "Elsa.GitHub.Gists.GetGist": ("gistId", "String"),
}
SEQUENCE_TYPE = "Elsa.Sequence"
UNSUPPORTED_CONTAINER_NAMES = {
    "Flowchart", "For", "ForEach", "ForEachV2", "Fork", "If", "Join", "Parallel", "StateMachine", "Switch", "While"
}
UNSUPPORTED_TOPOLOGY_PROPERTIES = {"branches", "connections", "nodes"}
UNSUPPORTED_TOPOLOGY_PROPERTY_CASES = UNSUPPORTED_TOPOLOGY_PROPERTIES | {
    value.capitalize() for value in UNSUPPORTED_TOPOLOGY_PROPERTIES
}


@dataclass(frozen=True)
class DecimalToken:
    """A JSON decimal retained exactly as it appeared in the source document."""

    raw: str


class MigrationError(ValueError):
    """An input cannot be migrated without guessing or losing data."""


def _unique_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise MigrationError(f"Duplicate JSON object key: {key!r}")
        result[key] = value
    return result


def parse_json(text: str) -> Any:
    def reject_constant(value: str) -> None:
        raise MigrationError(f"Non-standard JSON numeric constant is not accepted: {value}")

    try:
        return json.loads(
            text,
            parse_float=DecimalToken,
            parse_constant=reject_constant,
            object_pairs_hook=_unique_object,
        )
    except (json.JSONDecodeError, RecursionError) as error:
        raise MigrationError(f"Invalid JSON: {error}") from error


def encode_json(value: Any) -> str:
    """Serialize the supported JSON tree without converting decimal tokens to float."""

    def write(item: Any, depth: int) -> str:
        if isinstance(item, DecimalToken):
            return item.raw
        if item is None:
            return "null"
        if item is True:
            return "true"
        if item is False:
            return "false"
        if isinstance(item, int):
            return str(item)
        if isinstance(item, str):
            return json.dumps(item, ensure_ascii=False)
        if isinstance(item, list):
            if not item:
                return "[]"
            padding = "  " * (depth + 1)
            closing = "  " * depth
            parts = [padding + write(child, depth + 1) for child in item]
            return "[\n" + ",\n".join(parts) + "\n" + closing + "]"
        if isinstance(item, dict):
            if not item:
                return "{}"
            padding = "  " * (depth + 1)
            closing = "  " * depth
            parts = [
                padding + json.dumps(key, ensure_ascii=False) + ": " + write(child, depth + 1)
                for key, child in item.items()
            ]
            return "{\n" + ",\n".join(parts) + "\n" + closing + "}"
        raise MigrationError(f"Unsupported value in JSON tree: {type(item).__name__}")

    return write(value, 0) + "\n"


def _escape_pointer_segment(value: str) -> str:
    return value.replace("~", "~0").replace("/", "~1")


def _workflow_activity_nodes(workflow: Any) -> list[tuple[str, dict[str, Any]]]:
    if not isinstance(workflow, dict):
        raise MigrationError("Supported input must be an exported workflow object with a root activity")
    root_keys = [key for key in ("root", "Root") if key in workflow]
    if len(root_keys) != 1 or not isinstance(workflow[root_keys[0]], dict):
        raise MigrationError("Supported input must be an exported workflow object with a root activity")
    if any(name in workflow for name in ("activities", "Activities")):
        raise MigrationError(
            "Ambiguous workflow shape: exported workflow input must use Root/root, "
            "not a top-level activities array"
        )
    if any(
        isinstance(workflow.get(name), (dict, list))
        for name in UNSUPPORTED_TOPOLOGY_PROPERTY_CASES
    ):
        raise MigrationError(
            "Unsupported workflow topology at /; only a single root activity and Elsa.Sequence children are supported"
        )

    found: list[tuple[str, dict[str, Any]]] = []

    def visit(node: Any, pointer: str) -> None:
        if not isinstance(node, dict) or not isinstance(node.get("type"), str):
            raise MigrationError(f"Activity at {pointer} must be an object with a string type")
        type_name = node["type"]
        found.append((pointer, node))

        if type_name == SEQUENCE_TYPE:
            if any(
                isinstance(node.get(name), (dict, list))
                for name in UNSUPPORTED_TOPOLOGY_PROPERTY_CASES
            ):
                raise MigrationError(
                    f"Unsupported workflow topology at {pointer}: only ordered Elsa.Sequence children are supported"
                )
            children = node.get("activities", [])
            if not isinstance(children, list):
                raise MigrationError(f"Sequence activities at {pointer} must be an array")
            for index, child in enumerate(children):
                visit(child, pointer + "/activities/" + str(index))
            return

        short_name = type_name.rsplit(".", 1)[-1]
        if (type_name.startswith("Elsa.") and short_name in UNSUPPORTED_CONTAINER_NAMES) or any(
            isinstance(node.get(property_name), (dict, list))
            for property_name in UNSUPPORTED_TOPOLOGY_PROPERTY_CASES
        ):
            raise MigrationError(
                f"Unsupported workflow container at {pointer}: {type_name}; "
                "only a single root activity and Elsa.Sequence children are supported"
            )
        if "activities" in node and isinstance(node["activities"], (dict, list)):
            raise MigrationError(
                f"Unsupported nested activity topology at {pointer}: {type_name}; "
                "only Elsa.Sequence children are supported"
            )

    root_key = root_keys[0]
    visit(workflow[root_key], "/" + root_key)
    return found


def _is_integer(value: Any) -> bool:
    return isinstance(value, int) and not isinstance(value, bool)


def _activity_version(node: dict[str, Any], pointer: str) -> int | None:
    if "version" not in node:
        return None
    version = node["version"]
    if not _is_integer(version):
        raise MigrationError(f"Activity at {pointer or '/'} has a non-integer version")
    return version


def _existing_activity_ids(activity_nodes: list[tuple[str, dict[str, Any]]]) -> set[str]:
    ids: set[str] = set()
    for _, node in activity_nodes:
        node_id = node.get("id")
        if isinstance(node_id, str):
            ids.add(node_id)
    return ids


def _validate_mapping(mapping: Any, workflow_key: str, workflow_sha256: str) -> dict[str, str]:
    if not isinstance(mapping, dict):
        raise MigrationError("Mapping document must be an object")
    if set(mapping) != {"workflow_key", "workflow_sha256", "activity_ids"}:
        raise MigrationError("Mapping must contain only workflow_key, workflow_sha256, and activity_ids")
    if mapping["workflow_key"] != workflow_key:
        raise MigrationError("Mapping workflow_key does not match the requested workflow")
    expected_digest = mapping["workflow_sha256"]
    if not isinstance(expected_digest, str) or not re.fullmatch(r"[0-9a-f]{64}", expected_digest):
        raise MigrationError("Mapping workflow_sha256 must be a lowercase SHA-256 hex digest")
    if expected_digest != workflow_sha256:
        raise MigrationError("Mapping workflow_sha256 does not match the exact input workflow bytes")
    activity_ids = mapping["activity_ids"]
    if not isinstance(activity_ids, dict) or any(not isinstance(key, str) for key in activity_ids):
        raise MigrationError("activity_ids must map JSON Pointer strings to activity IDs")
    for pointer, activity_id in activity_ids.items():
        if not isinstance(activity_id, str) or not activity_id or activity_id != activity_id.strip():
            raise MigrationError(f"Activity ID for {pointer!r} must be a non-empty trimmed string")
        if any(ord(character) < 32 for character in activity_id):
            raise MigrationError(f"Activity ID for {pointer!r} contains a control character")
    if len(set(activity_ids.values())) != len(activity_ids):
        raise MigrationError("New activity IDs must be unique within the workflow")
    return activity_ids


def migrate_document(document: Any, mapping: Any, workflow_key: str, workflow_sha256: str) -> Any:
    if not isinstance(workflow_key, str) or not workflow_key.strip():
        raise MigrationError("workflow_key must be a non-empty string")
    activity_ids = _validate_mapping(mapping, workflow_key, workflow_sha256)
    candidates: dict[str, tuple[dict[str, Any], str, str]] = {}

    activity_nodes = _workflow_activity_nodes(document)
    for pointer, node in activity_nodes:
        type_name = node.get("type")
        if not isinstance(type_name, str) or type_name not in LEGACY_TYPES:
            continue
        version = _activity_version(node, pointer)
        if version == 2:
            continue
        if version not in (None, 1):
            raise MigrationError(f"Unsupported {type_name} version {version} at {pointer or '/'}")

        provider_property, expected_type = LEGACY_TYPES[type_name]
        provider_input = node.get("id")
        if not isinstance(provider_input, dict):
            raise MigrationError(
                f"Legacy provider input at {pointer or '/'} is missing or ambiguous; "
                "supply a complete legacy activity object before migration"
            )
        if provider_input.get("typeName") != expected_type or not isinstance(provider_input.get("expression"), dict):
            raise MigrationError(f"Legacy provider input wrapper is invalid at {pointer or '/'}")
        if provider_property in node:
            raise MigrationError(f"Target provider input {provider_property!r} already exists at {pointer or '/'}")
        candidates[pointer] = (node, provider_property, expected_type)

    candidate_paths = set(candidates)
    supplied_paths = set(activity_ids)
    missing = candidate_paths - supplied_paths
    unused = supplied_paths - candidate_paths
    if missing:
        raise MigrationError("Missing explicit activity IDs for JSON Pointers: " + ", ".join(sorted(missing)))
    if unused:
        raise MigrationError("Unused activity ID mappings for JSON Pointers: " + ", ".join(sorted(unused)))

    existing_ids = _existing_activity_ids(activity_nodes)
    collisions = existing_ids.intersection(activity_ids.values())
    if collisions:
        raise MigrationError("New activity IDs collide with existing workflow activity IDs: " + ", ".join(sorted(collisions)))

    migrated = copy.deepcopy(document)
    migrated_nodes = dict(_workflow_activity_nodes(migrated))
    for pointer, (_, provider_property, _) in candidates.items():
        node = migrated_nodes[pointer]
        provider_input = node.pop("id")
        node[provider_property] = provider_input
        node["id"] = activity_ids[pointer]
        node["version"] = 2
    return migrated


def _read_json(path: Path) -> Any:
    try:
        return parse_json(path.read_text(encoding="utf-8"))
    except OSError as error:
        raise MigrationError(f"Cannot read {path}: {error}") from error


def _atomic_create_new(path: Path, content: str) -> None:
    if path.exists() or path.is_symlink():
        raise MigrationError(f"Output already exists: {path}")
    parent = path.parent.resolve(strict=True)
    if not parent.is_dir():
        raise MigrationError(f"Output parent is not a directory: {parent}")
    temp_path: Path | None = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="w", encoding="utf-8", newline="\n", dir=parent, prefix=".elsa-github-id-", delete=False
        ) as temporary:
            temp_path = Path(temporary.name)
            temporary.write(content)
            temporary.flush()
            os.fsync(temporary.fileno())
        os.link(temp_path, path)
        directory_fd = os.open(parent, os.O_RDONLY)
        try:
            os.fsync(directory_fd)
        finally:
            os.close(directory_fd)
    except FileExistsError as error:
        raise MigrationError(f"Output already exists: {path}") from error
    except OSError as error:
        raise MigrationError(f"Cannot create output {path}: {error}") from error
    finally:
        if temp_path is not None:
            temp_path.unlink(missing_ok=True)


def migrate_files(input_path: Path, mapping_path: Path, output_path: Path, workflow_key: str) -> None:
    input_path = input_path.resolve(strict=True)
    mapping_path = mapping_path.resolve(strict=True)
    if output_path.exists() or output_path.is_symlink():
        raise MigrationError(f"Output already exists: {output_path}")
    output_parent = output_path.parent.resolve(strict=True)
    output_path = output_parent / output_path.name
    if output_path in (input_path, mapping_path):
        raise MigrationError("Output must be a new file distinct from the input and mapping")
    input_bytes = input_path.read_bytes()
    workflow_sha256 = hashlib.sha256(input_bytes).hexdigest()
    try:
        document = parse_json(input_bytes.decode("utf-8"))
    except UnicodeDecodeError as error:
        raise MigrationError(f"Input workflow is not UTF-8: {error}") from error
    mapping = _read_json(mapping_path)
    migrated = migrate_document(document, mapping, workflow_key, workflow_sha256)
    _atomic_create_new(output_path, encode_json(migrated))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True, type=Path, help="Workflow JSON to migrate")
    parser.add_argument("--mapping", required=True, type=Path, help="Explicit workflow-key and activity-ID map")
    parser.add_argument("--workflow-key", required=True, help="Workflow identity used to bind the supplied map")
    parser.add_argument("--output", required=True, type=Path, help="New output path; existing files are never replaced")
    args = parser.parse_args(argv)
    try:
        migrate_files(args.input, args.mapping, args.output, args.workflow_key)
    except (MigrationError, OSError) as error:
        print(f"Migration refused: {error}", file=sys.stderr)
        return 2
    print(f"Migrated workflow written to {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

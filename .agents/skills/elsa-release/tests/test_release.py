from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path
from subprocess import CompletedProcess
from unittest.mock import patch


SCRIPT_DIR = Path(__file__).resolve().parents[1] / "scripts"
sys.path.insert(0, str(SCRIPT_DIR))

import release  # noqa: E402
from release_support import parse_version  # noqa: E402


class ReleaseVersionTests(unittest.TestCase):
    def test_supported_versions_and_channel_properties(self) -> None:
        stable = parse_version("3.9.0")
        rc = parse_version("3.9.0-rc1")
        dotted_rc = parse_version("3.9.0-rc.1", "rc")
        preview = parse_version("3.9.0-preview.1")
        generic = parse_version("3.9.0-beta.1")

        self.assertEqual((stable.base, stable.kind, stable.prerelease, stable.npm_tag), ("3.9.0", "stable", False, "latest"))
        self.assertEqual((rc.base, rc.kind, rc.prerelease, rc.npm_tag), ("3.9.0", "rc", True, "next"))
        self.assertEqual(dotted_rc.version, "3.9.0-rc.1")
        self.assertEqual(preview.kind, "preview")
        self.assertEqual(generic.kind, "preview")

    def test_invalid_and_mismatched_versions_are_rejected(self) -> None:
        for value in ("3.09.0", "3.9", "v3.9.0", "3.9.0+build.1", "3.9.0-rc.01"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                parse_version(value)

        with self.assertRaises(ValueError):
            parse_version("3.9.0-rc1", "preview")
        with self.assertRaises(ValueError):
            parse_version("3.9.0-preview.1", "rc")
        with self.assertRaises(ValueError):
            parse_version("3.9.0", "preview")


class ReleaseSafetyTests(unittest.TestCase):
    def test_default_source_is_release_branch_for_base_version(self) -> None:
        version = parse_version("3.9.0-rc1")
        self.assertEqual(release.intended_source_ref(version), "origin/release/3.9.0")
        self.assertEqual(release.intended_source_ref(version, "upstream"), "upstream/release/3.9.0")

    def test_remote_source_uses_fresh_remote_sha_and_rejects_stale_local_ref(self) -> None:
        with patch.object(release, "git_remote", return_value=f"{'a' * 40}\trefs/heads/release/3.9.0"), patch.object(release, "git", return_value="b" * 40):
            with self.assertRaises(SystemExit):
                release.resolve_source_commit(Path("."), "origin/release/3.9.0", "origin")

    def test_remote_only_source_sha_is_usable_before_fetch(self) -> None:
        with patch.object(release, "git_remote", return_value=f"{'a' * 40}\trefs/heads/release/3.9.0"), patch.object(release, "git", return_value=""):
            self.assertEqual(release.resolve_source_commit(Path("."), "origin/release/3.9.0", "origin"), "a" * 40)

    def test_remote_only_object_does_not_turn_preflight_into_a_mutation(self) -> None:
        completed = CompletedProcess(["git"], 128, "", "fatal: malformed object name aaaa")
        with tempfile.TemporaryDirectory() as directory, patch.object(release.subprocess, "run", return_value=completed):
            self.assertEqual(release.remote_branches_containing(Path(directory), "a" * 40), [])

    def test_stable_rc_source_with_wrong_base_fails_before_any_mutation(self) -> None:
        with patch.object(sys, "argv", ["release.py", "--tag", "3.9.0", "--release-kind", "stable", "--source-ref", "3.8.0-rc1", "--execute"]), patch.object(release, "run") as run:
            with self.assertRaises(SystemExit):
                release.main()
        run.assert_not_called()

    def test_containment_accepts_origin_and_configured_remote(self) -> None:
        release.validate_containing_branches(["origin/main"], False, remote="upstream")
        release.validate_containing_branches(["upstream/release/3.9.0"], False, remote="upstream")
        with self.assertRaises(SystemExit):
            release.validate_containing_branches(["fork/main"], False, remote="upstream")

    def test_existing_tag_mismatch_is_rejected_without_retagging(self) -> None:
        with self.assertRaises(SystemExit):
            release.validate_existing_tag("3.9.0", "a" * 40, "b" * 40, "b" * 40)

    def test_matching_existing_release_is_reused_without_create_or_edit(self) -> None:
        release_metadata = release.ExistingRelease("3.9.0", "a" * 40, False, False)
        args = ["release.py", "--tag", "3.9.0", "--release-kind", "stable", "--github-repo", "owner/repo", "--execute"]
        with patch.object(sys, "argv", args), patch.object(release, "ensure_tool"), patch.object(release, "ensure_git_repo"), patch.object(release, "resolve_source_commit", return_value="a" * 40), patch.object(release, "remote_branches_containing", return_value=["origin/main"]), patch.object(release, "local_tag_target", return_value="a" * 40), patch.object(release, "remote_tag_target", return_value="a" * 40), patch.object(release, "verify_github_repo", return_value=True), patch.object(release, "inspect_release", return_value=release_metadata), patch.object(release, "run") as run:
            self.assertEqual(release.main(), 0)
        run.assert_not_called()

    def test_legacy_release_with_branch_target_metadata_reuses_matching_remote_tag(self) -> None:
        expected = parse_version("3.9.0")
        release.validate_existing_release(
            release.ExistingRelease("3.9.0", "main", False, False),
            "3.9.0",
            "a" * 40,
            expected,
            "a" * 40,
        )

    def test_kind_and_draft_mismatches_fail_for_existing_release(self) -> None:
        expected = parse_version("3.9.0")
        with self.assertRaises(SystemExit):
            release.validate_existing_release(release.ExistingRelease("3.9.0", "a" * 40, False, True), "3.9.0", "a" * 40, expected)
        with self.assertRaises(SystemExit):
            release.validate_existing_release(release.ExistingRelease("3.9.0", "a" * 40, True, False), "3.9.0", "a" * 40, expected)
        with self.assertRaises(SystemExit):
            release.validate_existing_release(release.ExistingRelease("3.9.0", "b" * 40, False, False), "3.9.0", "a" * 40, expected)

    def test_remote_error_is_not_treated_as_missing_tag(self) -> None:
        completed = CompletedProcess(["git"], 128, "", "fatal: could not read from remote repository")
        with tempfile.TemporaryDirectory() as directory, patch.object(release.subprocess, "run", return_value=completed):
            with self.assertRaises(SystemExit):
                release.git_remote(Path(directory), "origin", ["refs/tags/3.9.0"])

    def test_not_found_release_is_distinguished_from_authentication_error(self) -> None:
        self.assertTrue(release.is_github_not_found("", "release not found (HTTP 404)"))
        self.assertFalse(release.is_github_not_found("", "repository not found"))
        self.assertTrue(release.is_github_not_found("", "release not found", repository_visible=True))
        self.assertFalse(release.is_github_not_found("", "HTTP 401: authentication required"))
        self.assertFalse(release.is_github_not_found("", "HTTP 503: service unavailable"))

    def test_notes_version_metadata_must_match_requested_release(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            notes = Path(directory) / "notes.md"
            notes.write_text("<!-- elsa-release-version: 3.8.0 -->\n\n## Fixes\n", encoding="utf-8")
            with self.assertRaises(SystemExit):
                release.validate_notes_file(Path(directory), "notes.md", "3.9.0")


if __name__ == "__main__":
    unittest.main()

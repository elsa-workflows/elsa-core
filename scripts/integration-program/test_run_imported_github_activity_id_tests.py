import argparse
import subprocess
import tempfile
import unittest
from pathlib import Path

from run_github_activity_id_compatibility import PATCH_PATH, REPOSITORY, ProofError
from run_imported_github_activity_id_tests import (
    BOM,
    REFERENCE_MAP,
    TEST_PROJECT,
    port_differences,
    reviewed_files,
    run,
)


def new_file_section(path: str, *lines: str, final_newline: bool = True) -> bytes:
    body = "".join(f"+{line}\n" for line in lines)
    marker = "" if final_newline else "\\ No newline at end of file\n"
    return (
        f"diff --git a/{path} b/{path}\nnew file mode 100644\nindex 0000000..1111111\n"
        f"--- /dev/null\n+++ b/{path}\n@@ -0,0 +1,{len(lines)} @@\n{body}{marker}"
    ).encode()


class ImportedGitHubActivityIdRunnerTests(unittest.TestCase):
    def setUp(self):
        self.expected = reviewed_files(PATCH_PATH.read_bytes())

    def test_reviewed_patch_maps_only_to_imported_github_paths(self):
        self.assertEqual(6, len(self.expected))
        self.assertIn(TEST_PROJECT, self.expected)
        for path in self.expected:
            self.assertTrue(path.startswith(("src/extensions/devops/Elsa.DevOps.GitHub/", "test/extensions/modules/devops/")), path)
        project = self.expected[TEST_PROJECT]
        self.assertIn(REFERENCE_MAP[1], project)
        self.assertNotIn(b"src/modules/", project)

    def test_committed_port_matches_reviewed_patch_exactly(self):
        rows = port_differences(REPOSITORY, self.expected)
        self.assertEqual([], [row for row in rows if not row["matches"]])
        boms = {row["path"]: row["bom"] for row in rows}
        self.assertTrue(all(boms[path] for path in boms if path.startswith("src/")))
        self.assertFalse(any(boms[path] for path in boms if path.startswith("test/")))

    def test_modified_missing_or_non_leading_bom_content_is_rejected(self):
        path, content = next((path, content) for path, content in self.expected.items() if path.endswith("V2.cs"))
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            target = root / path
            target.parent.mkdir(parents=True)
            for data, matches in [
                (content, True),
                (BOM + content, True),
                (content.replace(b"CommentId", b"Id", 1) if b"CommentId" in content else content.replace(b"GistId", b"Id", 1), False),
                (content + BOM, False),
                (BOM + BOM + content, False),
            ]:
                with self.subTest(data=data[:12], matches=matches):
                    target.write_bytes(data)
                    self.assertEqual(matches, port_differences(root, {path: content})[0]["matches"])
            target.unlink()
            self.assertEqual("missing", port_differences(root, {path: content})[0]["reason"])

    def test_patch_sections_that_modify_or_escape_the_mapping_fail_closed(self):
        modified = b"diff --git a/src/modules/devops/A.cs b/src/modules/devops/A.cs\nindex 1..2 100644\n--- a/src/modules/devops/A.cs\n+++ b/src/modules/devops/A.cs\n@@ -1 +1 @@\n-a\n+b\n"
        with self.assertRaisesRegex(ProofError, "only add new regular files"):
            reviewed_files(modified)
        with self.assertRaisesRegex(ProofError, "unmapped path"):
            reviewed_files(new_file_section("src/modules/other/A.cs", "class A {}"))
        with self.assertRaisesRegex(ProofError, "exactly one mapped GitHub ProjectReference"):
            reviewed_files(new_file_section("test/modules/devops/T/T.csproj", "<Project />"))
        truncated = new_file_section("src/modules/devops/A.cs", "a", "b").replace(b"+1,2", b"+1,3")
        with self.assertRaisesRegex(ProofError, "line count"):
            reviewed_files(truncated)

    def test_final_newline_marker_is_preserved(self):
        files = reviewed_files(new_file_section("src/modules/devops/A.cs", "a", "", "b", final_newline=False))
        self.assertEqual(b"a\n\nb", files["src/extensions/devops/A.cs"])

    def test_dirty_source_fails_before_any_test_command(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "source"
            source.mkdir()
            subprocess.run(["git", "init", "-q", str(source)], check=True)
            (source / "untracked.txt").write_text("dirty", encoding="utf-8")
            output = root / "output"
            arguments = argparse.Namespace(source_tree=source, output_dir=output, dotnet="missing-dotnet", timeout_seconds=1)
            self.assertEqual(1, run(arguments))
            receipt = (output / "receipt.json").read_text(encoding="utf-8")
            self.assertIn("uncommitted changes", receipt)
            self.assertFalse((output / "net10-dotnet-test.log").exists())

    def test_output_inside_source_is_refused(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory)
            arguments = argparse.Namespace(source_tree=source, output_dir=source / "proof", dotnet="dotnet", timeout_seconds=1)
            with self.assertRaisesRegex(ProofError, "outside inspected source"):
                run(arguments)


if __name__ == "__main__":
    unittest.main()

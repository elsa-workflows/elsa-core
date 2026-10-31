"""Source-level guards for the package publication workflow.

These checks keep the recovery path fail-closed while the workflow itself is
validated with actionlint in CI or during release preparation.
"""

from pathlib import Path
import unittest


WORKFLOW = (
    Path(__file__).resolve().parents[1] / ".github" / "workflows" / "packages.yml"
)


def recovery_run_is_eligible(run: dict, jobs: list[dict]) -> bool:
    """Model the fail-closed run/job contract used by the recovery workflow."""

    if run.get("status") != "completed":
        return False
    if run.get("conclusion") not in {"success", "failure"}:
        return False

    build_jobs = [job for job in jobs if job.get("name") == "Build packages"]
    return (
        len(build_jobs) == 1
        and build_jobs[0].get("status") == "completed"
        and build_jobs[0].get("conclusion") == "success"
    )


def iter_block(lines: list[str], indent: int = 10):
    """Yield the lines of a YAML block scalar whose content is indented by ``indent`` spaces."""

    for line in lines:
        if line.strip() and len(line) - len(line.lstrip(" ")) < indent:
            return
        yield line


class PackagesWorkflowSourceGuardTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.workflow = WORKFLOW.read_text(encoding="utf-8")

    def test_nuget_publication_uses_oidc(self) -> None:
        self.assertIn("permissions:\n      actions: read\n      contents: read\n      id-token: write", self.workflow)
        self.assertIn("uses: NuGet/login@v1", self.workflow)
        self.assertIn("user: ${{ secrets.NUGET_USER }}", self.workflow)
        self.assertIn("steps.nuget_login.outputs.NUGET_API_KEY", self.workflow)
        self.assertNotIn("secrets.NUGET_API_KEY", self.workflow)

    def test_recovery_skips_build_and_reuses_original_artifact(self) -> None:
        self.assertIn("recovery_run_id:", self.workflow)
        self.assertIn("recovery_version:", self.workflow)
        self.assertIn(
            "github.event_name != 'workflow_dispatch' || inputs.recovery_run_id == ''",
            self.workflow,
        )
        self.assertIn("artifact-ids: ${{ steps.recovery_source.outputs.artifact_id }}", self.workflow)
        self.assertIn("merge-multiple: true", self.workflow)
        self.assertIn("github-token: ${{ github.token }}", self.workflow)
        self.assertIn("repository: ${{ github.repository }}", self.workflow)
        self.assertIn("name: elsa-template-packages", self.workflow)

    def test_recovery_validates_run_and_package_provenance_before_login(self) -> None:
        validation_start = self.workflow.index(
            "name: Validate original release run and resolve recovery artifact"
        )
        login_start = self.workflow.index("name: NuGet login (OIDC)")
        validation = self.workflow[validation_start:login_start]

        for required_check in (
            '.repository.full_name // empty',
            '.event // empty',
            '.status // empty',
            '.conclusion // empty',
            '.head_sha // empty',
            'select(.name == "Build packages")',
            'test "$(jq \'length\' <<<"${build_jobs}")" = "1"',
            ".[0].status // empty",
            ".[0].conclusion // empty",
            'actions/runs/${RECOVERY_RUN_ID}/artifacts?per_page=100',
            'select(.name == "elsa-template-packages" and .expired == false',
            'test "$(jq \'length\' <<<"${recovery_artifacts}")" = "1"',
            "artifact_digest=\"$(jq -r '.[0].digest // empty'",
            'tag_ref="refs/tags/${RECOVERY_VERSION}"',
            'git rev-parse --verify "${tag_ref}^{commit}"',
            'Elsa.Templates.${RECOVERY_VERSION}.nupkg',
            'repository commit does not match the immutable release tag',
        ):
            self.assertIn(required_check, validation)

        self.assertLess(validation_start, login_start)

        package_validation = self.workflow.index("name: Validate recovered package provenance")
        receipt = self.workflow.index("name: Write recovery receipt")
        evidence_upload = self.workflow.index("name: Upload recovery evidence")
        publish = self.workflow.index("- name: Publish to nuget.org")
        self.assertLess(package_validation, publish)
        self.assertLess(publish, receipt)
        self.assertLess(receipt, evidence_upload)
        for required_evidence_field in (
            '"schema": 1',
            '"repository":',
            '"version":',
            '"original_source_commit":',
            '"recovery_run_id":',
            '"recovery_workflow_sha":',
            '"original_release_run_id":',
            '"original_artifact":',
            '"digest":',
            '"target":',
            'name: elsa-template-recovery-evidence',
            'path: recovery-evidence/recovery-receipt.json',
        ):
            self.assertIn(required_evidence_field, self.workflow)

    def test_recovery_accepts_a_failed_run_when_build_succeeded(self) -> None:
        run = {
            "status": "completed",
            "conclusion": "failure",
        }
        jobs = [
            {"name": "Build packages", "status": "completed", "conclusion": "success"},
            {"name": "Publish to nuget.org", "status": "completed", "conclusion": "failure"},
        ]

        self.assertTrue(recovery_run_is_eligible(run, jobs))
        self.assertFalse(recovery_run_is_eligible({**run, "status": "in_progress"}, jobs))
        self.assertFalse(recovery_run_is_eligible({**run, "conclusion": "cancelled"}, jobs))
        self.assertFalse(
            recovery_run_is_eligible(
                run,
                jobs + [{"name": "Build packages", "status": "completed", "conclusion": "success"}],
            )
        )

    def _step(self, name: str) -> str:
        """Return the source of the workflow step called ``name``, up to the next step or job."""

        start = self.workflow.index(f"      - name: {name}\n")
        candidates = [
            index
            for index in (
                self.workflow.find("\n      - name: ", start + 1),
                self.workflow.find("\n  publish_", start + 1),
            )
            if index != -1
        ]
        end = min(candidates) if candidates else len(self.workflow)
        return self.workflow[start:end]

    def test_pushes_fail_fast_on_an_empty_api_key(self) -> None:
        for check_name, push_name, key_expression, push_env in (
            (
                "Check API key (feedz.io)",
                "Publish to feedz.io",
                "${{ secrets.FEEDZ_API_KEY }}",
                "FEEDZ_API_KEY",
            ),
            (
                "Check API key (nuget.org)",
                "Publish to nuget.org",
                "${{ steps.nuget_login.outputs.NUGET_API_KEY }}",
                "NUGET_API_KEY",
            ),
        ):
            with self.subTest(push=push_name):
                check = self._step(check_name)
                push = self._step(push_name)

                # The check reads exactly the key its push uses, through env.
                self.assertIn(f"API_KEY: {key_expression}", check)
                self.assertIn('if [ -z "$API_KEY" ]; then', check)
                self.assertIn("nothing was pushed.", check)
                self.assertIn("exit 1", check)
                self.assertIn(f"{push_env}: {key_expression}", push)

                # It runs immediately before the push, under the same conditions.
                self.assertLess(
                    self.workflow.index(f"      - name: {check_name}\n"),
                    self.workflow.index(f"      - name: {push_name}\n"),
                )
                between = self.workflow[
                    self.workflow.index(f"      - name: {check_name}\n") + len(check) :
                    self.workflow.index(f"      - name: {push_name}\n")
                ]
                self.assertEqual(between.strip(), "")
                self.assertNotIn("if:", check)
                self.assertNotIn("if:", push)

                # The push passes the key through env, never inline.
                run_lines = push[push.index("        run: |\n") :].splitlines()[1:]
                run_script = "\n".join(
                    line for line in iter_block(run_lines)
                )
                self.assertNotIn("${{", run_script)
                self.assertIn(f'--api-key "${{{push_env}}}"', run_script)

        self.assertIn(
            "Check the FEEDZ_API_KEY secret (repository or organization)",
            self._step("Check API key (feedz.io)"),
        )
        self.assertIn(
            "Check the NuGet login (OIDC) step output and the Trusted Publishing policy on nuget.org",
            self._step("Check API key (nuget.org)"),
        )

    def test_normal_release_still_publishes_from_successful_build(self) -> None:
        self.assertIn("github.event_name == 'release' && needs.build.result == 'success'", self.workflow)
        self.assertIn("if: ${{ github.event_name == 'release' }}", self.workflow)


if __name__ == "__main__":
    unittest.main()

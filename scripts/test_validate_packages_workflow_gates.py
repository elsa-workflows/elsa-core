import sys
import textwrap
import unittest
import unittest.mock
from pathlib import Path

import yaml

sys.path.insert(0, str(Path(__file__).resolve().parent))

import validate_packages_workflow_gates as gates

NUGET_IF = (
    "if: ${{ github.event_name == 'workflow_dispatch' && inputs.publish_nuget"
    " && startsWith(github.ref, 'refs/tags/') }}"
)
COVERAGE_REFS_IF = "github.ref == 'refs/heads/main' || github.ref == 'refs/heads/release/3.6.1') }}"
SELECTION_RUN = "        run: python3 scripts/validate_packages_publication_selection.py\n"

# (description, anchor to replace or None to append a job, replacement, expected violation)
OTHER_BYPASSES = (
    (
        "publish_nuget runs even when the selection check fails",
        NUGET_IF,
        NUGET_IF.replace("${{ ", "${{ always() && "),
        "publish_nuget if: must not call always()",
    ),
    (
        "publish_nuget dispatch requirement moved into an alternative",
        NUGET_IF,
        NUGET_IF.replace("${{ ", "${{ github.event_name == 'release' || (").replace(" }}", ") }}"),
        "publish_nuget if: must require github.event_name == 'workflow_dispatch'",
    ),
    (
        "Feedz publisher no longer needs its input",
        "inputs.publish_preview_feedz && (startsWith",
        "(startsWith",
        "publish_preview_feedz if: must require inputs.publish_preview_feedz",
    ),
    (
        "Feedz publisher accepts a stable 3.10.x tag",
        "startsWith(github.ref, 'refs/heads/') || (startsWith(github.ref, 'refs/tags/3.10.') && contains(github.ref_name, '-'))",
        "startsWith(github.ref, 'refs/heads/') || startsWith(github.ref, 'refs/tags/3.10.')",
        "publish_preview_feedz if: must require startsWith(github.ref, 'refs/heads/') || (startsWith(github.ref, 'refs/tags/3.10.') && contains(github.ref_name, '-'))",
    ),
    (
        "nuget publisher drops the candidate retrieve",
        "python3 scripts/stage_consolidated_candidate_nupkgs.py retrieve",
        "python3 scripts/stage_consolidated_candidate_nupkgs.py missing",
        "publish_nuget must retrieve the pinned candidate",
    ),
    (
        "nuget login runs before the candidate guard",
        "      - name: Retrieve verified candidate archive\n",
        "      - name: NuGet login too early\n        uses: NuGet/login@v1\n\n      - name: Retrieve verified candidate archive\n",
        "publish_nuget must retrieve, stage, and guard the pinned candidate before NuGet login",
    ),
    (
        "nuget publisher continues after a failed candidate guard",
        "        run: |\n          python3 scripts/stage_consolidated_candidate_nupkgs.py stage\n          python3 scripts/validate_packages_id_set.py\n",
        "        continue-on-error: true\n        run: |\n          python3 scripts/stage_consolidated_candidate_nupkgs.py stage\n          python3 scripts/validate_packages_id_set.py\n",
        "publish_nuget must not continue on error",
    ),
    (
        "stable 3.10 nuget still packs",
        "        if: ${{ !(github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')) }}\n        run: ./build.sh Compile+Pack",
        "        run: ./build.sh Compile+Pack",
        "build Compile+Pack must skip a stable 3.10.x nuget.org promotion",
    ),
    (
        "packages.yml packs the consolidated set again",
        "      - name: Compile+Pack\n",
        "      - name: Build Studio ClientLib bundles\n        run: bash scripts/integration-program/build_studio_clientlibs.sh\n\n      - name: Compile+Pack\n",
        "packages.yml must not build Studio ClientLibs",
    ),
    (
        "text around ${{ }} makes the coverage condition always true",
        COVERAGE_REFS_IF,
        COVERAGE_REFS_IF + " || true",
        "deploy_coverage if: has text outside ${{ }}",
    ),
    (
        "coverage allowlist differs from the selection check",
        COVERAGE_REFS_IF,
        COVERAGE_REFS_IF.replace(" || ", " || github.ref == 'refs/heads/develop/3.6.1' || "),
        "deploy_coverage if: must require github.ref == 'refs/heads/main' || github.ref == 'refs/heads/release/3.6.1'",
    ),
    (
        "Pages artifact no longer uploaded for an allowed coverage ref",
        "if: github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/heads/release/')",
        "if: github.ref == 'refs/heads/main'",
        "deploy_coverage accepts refs/heads/release/3.6.1, but no Pages artifact is uploaded for it",
    ),
    (
        "build no longer waits for a successful selection check",
        " && (github.event_name != 'workflow_dispatch' || needs.validate_publication_selection.result == 'success')",
        "",
        "build if: must require (github.event_name != 'workflow_dispatch' || needs.validate_publication_selection.result == 'success')",
    ),
    (
        "build selection gate turned into an alternative",
        " && (github.event_name != 'workflow_dispatch' || needs.validate_publication_selection.result == 'success')",
        " || (github.event_name != 'workflow_dispatch' || needs.validate_publication_selection.result == 'success')",
        "build if: must require",
    ),
    (
        "build drops the selection job from needs",
        "      - test_component\n      - validate_publication_selection\n",
        "      - test_component\n",
        "build must need validate_publication_selection",
    ),
    (
        "selection step continues on error",
        SELECTION_RUN,
        "        continue-on-error: true\n" + SELECTION_RUN,
        "validate_publication_selection must not continue on error",
    ),
    (
        "selection script result ignored",
        SELECTION_RUN,
        SELECTION_RUN.replace(".py", ".py || true"),
        "validate_publication_selection must run exactly",
    ),
    (
        "selection script sees a fixed ref",
        "          REF: ${{ github.ref }}\n          PUBLISH_PREVIEW_FEEDZ",
        "          REF: refs/heads/main\n          PUBLISH_PREVIEW_FEEDZ",
        "validate_publication_selection must pass REF: ${{ github.ref }}",
    ),
    (
        "workflow is callable by another workflow",
        "on:\n  workflow_dispatch:\n",
        "on:\n  workflow_call:\n  workflow_dispatch:\n",
        "must not be callable (workflow_call)",
    ),
    (
        "workflow-level env exposes the NuGet key to every job",
        "  base_version: '3.10.0'\n",
        "  base_version: '3.10.0'\n  NUGET_KEY: ${{ secrets.NUGET_API_KEY }}\n",
        "the workflow outside its jobs references secrets.NUGET*",
    ),
    (
        "workflow-level permissions grant OIDC tokens to every job",
        "\njobs:\n",
        "\npermissions:\n  id-token: write\n\njobs:\n",
        "the workflow outside its jobs references id-token: write",
    ),
    (
        "coverage report deploys Pages itself",
        "      - name: Upload Pages artifact\n",
        "      - name: Deploy\n        uses: actions/deploy-pages@v4\n\n      - name: Upload Pages artifact\n",
        "job 'coverage_report' references actions/deploy-pages",
    ),
    (
        "indexed, lower-case secret name",
        None,
        """
        leak:
          runs-on: ubuntu-latest
          steps:
            - run: ./publish.sh
              env:
                KEY: ${{ secrets['nuget_api_key'] }}
        """,
        "job 'leak' references secrets.NUGET*",
    ),
    (
        "whole secrets context",
        None,
        """
        leak:
          runs-on: ubuntu-latest
          steps:
            - run: echo '${{ toJSON(secrets) }}' > secrets.json
        """,
        "job 'leak' references the whole secrets context",
    ),
    (
        "OIDC token in an ungated job",
        None,
        """
        leak:
          runs-on: ubuntu-latest
          permissions:
            id-token: write
          steps:
            - run: echo ungated
        """,
        "job 'leak' references id-token: write",
    ),
    (
        "write-all permissions in an ungated job",
        None,
        """
        leak:
          runs-on: ubuntu-latest
          permissions: write-all
          steps:
            - run: echo ungated
        """,
        "job 'leak' references id-token: write",
    ),
    (
        "environment in an ungated job",
        None,
        """
        leak:
          runs-on: ubuntu-latest
          environment: github-pages
          steps:
            - run: echo ungated
        """,
        "job 'leak' references an environment",
    ),
    (
        "reusable workflow inheriting secrets",
        None,
        """
        leak:
          uses: ./.github/workflows/other.yml
          secrets: inherit
        """,
        "job 'leak' references secrets passed to a reusable workflow",
    ),
    (
        "indexed secret with a computed (non-literal) index",
        None,
        """
        leak:
          runs-on: ubuntu-latest
          steps:
            - run: ./publish.sh
              env:
                KEY: ${{ secrets[vars.PACKAGE_FEED_SECRET] }}
        """,
        "job 'leak' references an indexed secrets access",
    ),
    (
        "indexed, upper-case secret name",
        None,
        """
        leak:
          runs-on: ubuntu-latest
          steps:
            - run: ./publish.sh
              env:
                KEY: ${{ secrets['FEEDZ_API_KEY'] }}
        """,
        "job 'leak' references an indexed secrets access",
    ),
    (
        "push composite action in an ungated job",
        None,
        """
        leak:
          runs-on: ubuntu-latest
          steps:
            - uses: ./.github/actions/push-nuget-packages
              with:
                feed-source: https://example.invalid/index.json
                api-key: none
        """,
        "job 'leak' references the push-nuget-packages action",
    ),
)


class WorkflowGateTests(unittest.TestCase):
    def setUp(self):
        self.source = gates.WORKFLOW.read_text(encoding="utf-8")

    def mutated(self, old, new):
        if old is None:
            job = textwrap.indent(textwrap.dedent(new).strip("\n"), "  ")
            return self.source.rstrip("\n") + "\n\n" + job + "\n"
        self.assertEqual(1, self.source.count(old), f"test anchor must occur exactly once: {old!r}")
        return self.source.replace(old, new)

    def assert_rejected(self, source, expected):
        violations = gates.source_violations(source)
        self.assertTrue(any(expected in violation for violation in violations), f"{expected!r} not in {violations}")

    def test_real_workflow_passes(self):
        self.assertEqual([], gates.source_violations(self.source))

    def test_rejects_push_job_using_the_feedz_key(self):
        source = self.mutated(None, """
            sneak_feedz:
              runs-on: ubuntu-latest
              if: ${{ github.event_name == 'push' }}
              steps:
                - run: ./publish.sh
                  env:
                    FEEDZ_KEY: ${{ secrets.FEEDZ_API_KEY }}
            """)
        self.assert_rejected(source, "job 'sneak_feedz' references secrets.FEEDZ*")

    def test_rejects_ungated_job_using_nuget_login(self):
        source = self.mutated(None, """
            sneak_nuget:
              runs-on: ubuntu-latest
              steps:
                - uses: NuGet/login@v1
                  with:
                    user: someone
            """)
        self.assert_rejected(source, "job 'sneak_nuget' references NuGet/login")

    def test_rejects_package_push_in_build(self):
        source = self.mutated(
            "      - name: Upload artifact\n",
            "      - name: Push packages\n"
            "        run: dotnet nuget push \"packages/*.nupkg\" --source \"$FEED\"\n\n"
            "      - name: Upload artifact\n",
        )
        self.assert_rejected(source, "job 'build' references dotnet nuget push")

    def test_rejects_nuget_publisher_that_only_mentions_the_selection_job_in_a_comment(self):
        source = self.mutated(
            "    name: Publish release to nuget.org\n    needs: [build, validate_publication_selection]\n",
            "    name: Publish release to nuget.org\n    needs: [build] # validate_publication_selection\n",
        )
        self.assertEqual(self.source.count("validate_publication_selection"), source.count("validate_publication_selection"))
        self.assert_rejected(source, "publish_nuget must need validate_publication_selection")

    def test_rejects_publication_inputs_that_default_on(self):
        for name in gates.GATED_JOBS:
            with self.subTest(input=name):
                workflow = yaml.safe_load(self.source)
                gates.triggers(workflow)["workflow_dispatch"]["inputs"][name]["default"] = True
                self.assertIn(f"workflow_dispatch input {name!r} must default to false", gates.workflow_violations(workflow))

    def test_rejects_other_publication_bypasses(self):
        for description, old, new, expected in OTHER_BYPASSES:
            with self.subTest(description):
                self.assert_rejected(self.mutated(old, new), expected)

    def test_rejects_selection_script_that_allows_a_non_release_tag(self):
        with unittest.mock.patch.object(gates, "selection_errors", return_value=[]):
            self.assert_rejected(self.source, "the selection script must reject a v-prefixed tag")

    def test_accepts_ungated_job_whose_string_literal_merely_contains_secrets_bracket(self):
        source = self.mutated(None, """
            leak:
              runs-on: ubuntu-latest
              if: ${{ vars.MODE != 'secrets[disabled]' }}
              steps:
                - run: echo ok
            """)
        self.assertEqual([], gates.source_violations(source))

    def test_candidate_publisher_must_run_the_same_id_set_guard_before_feedz(self):
        source = gates.CANDIDATE_WORKFLOW.read_text(encoding="utf-8")
        self.assertEqual([], gates.candidate_publisher_violations(source))
        stripped = source.replace(gates.GUARD_CANDIDATE, "true")
        self.assertIn(
            "publish-consolidated-original-candidate.yml must run the same ID-set guard before Feedz",
            gates.candidate_publisher_violations(stripped),
        )
        reordered = source.replace(gates.GUARD_CANDIDATE, "GUARD_PLACEHOLDER").replace(
            gates.CANDIDATE_PUBLISH, f"{gates.CANDIDATE_PUBLISH}\n          {gates.GUARD_CANDIDATE}"
        ).replace("GUARD_PLACEHOLDER", "true")
        self.assertIn(
            "publish-consolidated-original-candidate.yml must run the ID-set guard before the Feedz publish",
            gates.candidate_publisher_violations(reordered),
        )


if __name__ == "__main__":
    unittest.main()

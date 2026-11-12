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
        "        if: ${{ !((github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')) || ((github.event_name == 'release' || github.event_name == 'workflow_dispatch') && github.ref == 'refs/tags/3.10.0')) }}\n        run: ./build.sh Compile+Pack",
        "        run: ./build.sh Compile+Pack",
        "build Compile+Pack must skip a stable 3.10.x nuget.org promotion and the 3.10.0 candidate pack",
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
        "          REF: ${{ github.ref }}\n          PUBLISH_PREVIEW_FEEDZ: ${{ inputs.publish_preview_feedz }}\n          PUBLISH_NUGET: ${{ inputs.publish_nuget }}\n          PUBLISH_NPM: ${{ inputs.publish_npm }}\n          DEPLOY_COVERAGE: ${{ inputs.deploy_coverage }}\n          EXCLUDE_IDS: ${{ inputs.exclude_ids }}",
        "          REF: refs/heads/main\n          PUBLISH_PREVIEW_FEEDZ: ${{ inputs.publish_preview_feedz }}\n          PUBLISH_NUGET: ${{ inputs.publish_nuget }}\n          PUBLISH_NPM: ${{ inputs.publish_npm }}\n          DEPLOY_COVERAGE: ${{ inputs.deploy_coverage }}\n          EXCLUDE_IDS: ${{ inputs.exclude_ids }}",
        "validate_publication_selection must pass REF: ${{ github.ref }}",
    ),
    (
        "nuget publisher drops Download Packages",
        "      - name: Download Packages\n        # Pinned for the reason given on the checkout above. Skipped for stable\n",
        "      - name: Fetch packages\n        # Pinned for the reason given on the checkout above. Skipped for stable\n",
        "publish_nuget must contain Download Packages",
    ),
    (
        "build drops Upload artifact",
        "      - name: Upload artifact\n",
        "      - name: Upload packages\n",
        "build job must contain Upload artifact",
    ),
    (
        "build drops setup-dotnet",
        "      - uses: actions/setup-dotnet@v4\n        if: ${{ !(github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')) }}\n        with:\n          dotnet-version: |\n            8.0.x\n            9.0.x\n            10.x\n",
        "      - uses: actions/setup-python@v5\n        if: ${{ !(github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')) }}\n        with:\n          python-version: '3.12'\n",
        "build job must contain setup-dotnet",
    ),
    (
        "nuget guard hard-codes EXCLUDE_IDS",
        "          PACKAGES_DIR: ${{ github.workspace }}\n          EXCLUDE_IDS: ${{ inputs.exclude_ids }}\n",
        "          PACKAGES_DIR: ${{ github.workspace }}\n          EXCLUDE_IDS: Elsa.Studio.Workflows.Designer\n",
        "publish_nuget candidate guard env EXCLUDE_IDS must be ${{ inputs.exclude_ids }}",
    ),
    (
        "nuget publisher sets EXCLUDE_IDS at job level",
        "    if: ${{ github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/') }}\n    permissions:\n",
        "    if: ${{ github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/') }}\n    env:\n      EXCLUDE_IDS: Elsa.Studio.Workflows.Designer\n    permissions:\n",
        "publish_nuget must not set EXCLUDE_IDS at job level",
    ),
    (
        "packages.yml sets EXCLUDE_IDS at workflow level",
        "  base_version: '3.10.0'\n",
        "  base_version: '3.10.0'\n  EXCLUDE_IDS: Elsa.Studio.Workflows.Designer\n",
        "packages.yml must not set EXCLUDE_IDS at workflow level",
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

    # --- Candidate promotion (ported from the publish-consolidated-original-candidate.yml guard tests) ---
    FEEDZ_GUARD_NAME = "      - name: Guard the full ID set before Feedz\n"
    FEEDZ_GUARD_RUN_BLOCK = (
        "          set -euo pipefail\n"
        "          python3 scripts/validate_packages_id_set.py\n"
        '          python3 scripts/verify_consolidated_candidate.py count --version "$VERSION" --feed feedz --dir "$PACKAGES_DIR"\n'
    )
    FEEDZ_GUARD_RUN_VIOLATION = (
        "publish_candidate_feedz guard run: must be exactly set -euo pipefail, the ID-set guard and the pinned count"
    )

    def candidate_violations(self, source):
        return gates.candidate_promotion_violations(source)

    def feedz_guard_with(self, extra_line):
        return self.mutated(self.FEEDZ_GUARD_NAME, f"{self.FEEDZ_GUARD_NAME}        {extra_line}\n")

    def mutate_feedz_guard_run(self, new_block):
        return self.mutated(self.FEEDZ_GUARD_RUN_BLOCK, new_block)

    def test_real_candidate_promotion_passes(self):
        self.assertEqual([], self.candidate_violations(self.source))

    def test_candidate_publisher_must_run_the_same_id_set_guard_before_feedz(self):
        guard_job = "publish_candidate_feedz must run the ID-set guard before publishing"
        stripped = self.mutated(
            self.FEEDZ_GUARD_RUN_BLOCK,
            "          set -euo pipefail\n          true\n",
        )
        self.assertTrue(any(guard_job in v for v in self.candidate_violations(stripped)))
        reordered = self.mutated(
            "      - name: Publish the candidate to feedz.io\n        uses: ./.github/actions/push-nuget-packages\n"
            "        with:\n          feed-source: ${{ env.feedz_feed_source }}\n"
            "          api-key: ${{ secrets.ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY }}\n",
            "",
        )
        self.assertTrue(any("must publish with the push-nuget-packages action" in v for v in self.candidate_violations(reordered)))
        moved = self.source.replace(self.FEEDZ_GUARD_NAME, "      - name: Guard moved\n", 1)
        self.assertEqual([], [v for v in self.candidate_violations(moved) if "Guard" in v])

    def test_candidate_guard_rejects_continue_on_error(self):
        self.assertIn(
            "candidate promotion: publish_candidate_feedz must not continue on error",
            self.candidate_violations(self.feedz_guard_with("continue-on-error: true")),
        )

    def test_candidate_guard_allows_continue_on_error_false(self):
        self.assertEqual([], self.candidate_violations(self.feedz_guard_with("continue-on-error: false")))

    def test_candidate_guard_rejects_an_if_condition(self):
        for extra in ("if: always()", "if: false", "if: ''", "if: 0"):
            with self.subTest(extra=extra):
                self.assertIn(
                    "candidate promotion: publish_candidate_feedz steps must not have an if:; every version runs the same steps",
                    self.candidate_violations(self.feedz_guard_with(extra)),
                )

    def test_candidate_guard_rejects_failure_suppressing_run_forms(self):
        for suffix in ("||true", "|| :", "|| exit 0", "; true", "|| echo ok"):
            with self.subTest(suffix=suffix):
                mutated = self.mutate_feedz_guard_run(
                    self.FEEDZ_GUARD_RUN_BLOCK.replace("--dir \"$PACKAGES_DIR\"\n", f"--dir \"$PACKAGES_DIR\" {suffix}\n")
                )
                self.assertTrue(any(self.FEEDZ_GUARD_RUN_VIOLATION in v for v in self.candidate_violations(mutated)))
                mutated = self.mutate_feedz_guard_run(
                    self.FEEDZ_GUARD_RUN_BLOCK.replace("id_set.py\n", f"id_set.py {suffix}\n")
                )
                self.assertTrue(any(self.FEEDZ_GUARD_RUN_VIOLATION in v for v in self.candidate_violations(mutated)))

    def test_candidate_guard_rejects_set_plus_e_before_the_guard(self):
        mutated = self.mutate_feedz_guard_run(self.FEEDZ_GUARD_RUN_BLOCK.replace("set -euo pipefail\n", "set -euo pipefail\n          set +e\n"))
        self.assertTrue(any(self.FEEDZ_GUARD_RUN_VIOLATION in v for v in self.candidate_violations(mutated)))

    def test_candidate_guard_accepts_harmless_run_whitespace(self):
        mutated = self.mutate_feedz_guard_run(
            "          set  -euo   pipefail  \n"
            "\n"
            "          python3 scripts/validate_packages_id_set.py   \n"
            '          python3 scripts/verify_consolidated_candidate.py count --version "$VERSION" --feed feedz --dir "$PACKAGES_DIR"\n'
        )
        self.assertEqual([], self.candidate_violations(mutated))

    def test_candidate_guard_rejects_folded_run(self):
        mutated = self.mutated("        run: |\n" + self.FEEDZ_GUARD_RUN_BLOCK, "        run: >-\n" + self.FEEDZ_GUARD_RUN_BLOCK)
        self.assertTrue(any(self.FEEDZ_GUARD_RUN_VIOLATION in v for v in self.candidate_violations(mutated)))

    def test_candidate_guard_rejects_shell(self):
        self.assertTrue(any(
            "publish_candidate_feedz guard step keys must be among name, env, run, id, if, continue-on-error" in v
            for v in self.candidate_violations(self.feedz_guard_with("shell: true {0}"))
        ))

    def test_candidate_guard_rejects_workflow_defaults_shell(self):
        mutated = self.mutated("\njobs:\n", "\ndefaults:\n  run:\n    shell: bash\n\njobs:\n")
        self.assertIn("candidate promotion: must not set defaults.run.shell", self.candidate_violations(mutated))

    def test_candidate_guard_rejects_job_defaults_shell(self):
        anchor = "    name: Publish the pinned candidate to Feedz\n"
        mutated = self.mutated(anchor, anchor + "    defaults:\n      run:\n        shell: bash\n")
        self.assertIn("candidate promotion: must not set defaults.run.shell", self.candidate_violations(mutated))

    def test_candidate_publishers_reject_a_changed_ref_requirement(self):
        # Replaces the old fixed REF env: every candidate job must require refs/heads/main.
        for ref in ("refs/tags/3.10.1", "refs/heads/release/3.10.0"):
            with self.subTest(ref=ref):
                mutated = self.source.replace("test \"$GITHUB_REF\" = 'refs/heads/main'", f"test \"$GITHUB_REF\" = '{ref}'")
                self.assertTrue(any("must require the repository, refs/heads/main and run attempt 1" in v
                                    for v in self.candidate_violations(mutated)))

    def test_candidate_publisher_rejects_exclude_ids_for_feedz_and_unpinned_ones_for_nuget(self):
        mutated = self.mutated(
            self.FEEDZ_GUARD_NAME + "        env:\n          PACKAGES_DIR: ${{ github.workspace }}\n",
            self.FEEDZ_GUARD_NAME + "        env:\n          PACKAGES_DIR: ${{ github.workspace }}\n          EXCLUDE_IDS: Elsa\n",
        )
        self.assertIn("candidate promotion: publish_candidate_feedz must not set EXCLUDE_IDS", self.candidate_violations(mutated))
        nuget = self.source.replace("EXCLUDE_IDS: ${{ steps.pin.outputs.exclude_ids }}", "EXCLUDE_IDS: Elsa")
        self.assertIn("candidate promotion: publish_candidate_nuget guard EXCLUDE_IDS must be the pinned exclusions",
                      self.candidate_violations(nuget))

    def test_candidate_steps_reject_status_functions(self):
        mutated = self.mutated(self.FEEDZ_GUARD_NAME, f"{self.FEEDZ_GUARD_NAME}        if: always()\n")
        self.assertTrue(any("must not call always(), failure() or cancelled()" in v for v in self.candidate_violations(mutated)))

    def test_candidate_jobs_reject_skip_overrides_and_tagging(self):
        for text in ("skip_id_set", "BYPASS_GUARD"):
            with self.subTest(text=text):
                mutated = self.mutated(self.FEEDZ_GUARD_RUN_BLOCK, self.FEEDZ_GUARD_RUN_BLOCK + f"          echo {text}\n")
                self.assertIn("candidate promotion: must not offer a skip, bypass, or ID-list override", self.candidate_violations(mutated))
        mutated = self.mutated(self.FEEDZ_GUARD_RUN_BLOCK, self.FEEDZ_GUARD_RUN_BLOCK + "          git tag 3.10.1\n")
        self.assertTrue(any("must not create or push a tag" in v for v in self.candidate_violations(mutated)))

    def test_candidate_publishers_must_verify_tokenless_before_the_key_or_login(self):
        blank = "          GITHUB_TOKEN: ''\n          GH_TOKEN: ''\n          ACTIONS_READ_TOKEN: ''\n"
        self.assertEqual(2, self.source.count(blank))
        mutated = self.source.replace(blank, "          GH_TOKEN: ''\n          ACTIONS_READ_TOKEN: ''\n")
        self.assertEqual(2, sum("verify step must blank" in v for v in self.candidate_violations(mutated)))
        mutated = self.source.replace(blank, blank.replace("ACTIONS_READ_TOKEN: ''", "ACTIONS_READ_TOKEN: x"))
        self.assertEqual(2, sum("verify step must blank" in v for v in self.candidate_violations(mutated)))

    def test_candidate_environments_retrieve_and_permissions_are_enforced(self):
        anchor = "    environment: feedz-publish\n    # One feedz promotion"
        self.assertEqual(1, self.source.count(anchor))
        mutated = self.source.replace(anchor, "    environment: other\n    # One feedz promotion")
        self.assertIn("candidate promotion: publish_candidate_feedz must use environment feedz-publish", self.candidate_violations(mutated))
        mutated = self.source.replace("    name: Retrieve and verify the pinned candidate\n", "    name: Retrieve and verify the pinned candidate\n    environment: feedz-publish\n")
        self.assertTrue(any("retrieve_candidate must have no environment" in v for v in self.candidate_violations(mutated)))
        mutated = self.source.replace("    needs: validate_publication_selection\n    if: ${{ github.event_name == 'workflow_dispatch' && inputs.promote_candidate_version != '' }}",
                                       "    needs: validate_publication_selection\n    if: ${{ inputs.promote_candidate_version != '' }}")
        self.assertTrue(any("validate_candidate_pin if: must be exactly" in v for v in self.candidate_violations(mutated)))
        mutated = self.source.replace("run: python3 scripts/verify_consolidated_candidate.py validate-pin", "run: true # validate-pin")
        self.assertTrue(any("first run step of validate_candidate_pin must validate the pin" in v for v in self.candidate_violations(mutated)))

    def test_candidate_jobs_reject_unpinned_actions_and_version_literals(self):
        pinned = "actions/setup-python@a26af69be951a213d495a4c3e4e4022e16d87065 # v5"
        last = self.source.rindex(pinned)  # the candidate jobs are the last jobs in the workflow
        mutated = self.source[:last] + "actions/setup-python@v5" + self.source[last + len(pinned):]
        self.assertTrue(any("must be pinned to a full commit SHA" in v for v in self.candidate_violations(mutated)))
        mutated = self.mutated(self.FEEDZ_GUARD_RUN_BLOCK, self.FEEDZ_GUARD_RUN_BLOCK + "          echo 3.10.1\n")
        self.assertTrue(any("must not name a version" in v for v in self.candidate_violations(mutated)))

    def test_candidate_promotion_input_must_be_a_string_defaulting_to_empty(self):
        mutated = self.source.replace("      promote_candidate_version:\n        description:", "      promote_candidate_version:\n        x: 1\n        description:")
        self.assertEqual([], self.candidate_violations(mutated))
        mutated = self.source.replace("        type: string\n        default: ''\n  push:", "        type: boolean\n        default: false\n  push:")
        self.assertTrue(any("promote_candidate_version' must be a string" in v for v in self.candidate_violations(mutated)))

    def test_candidate_publishers_are_not_flagged_by_the_capability_scan_but_other_jobs_still_are(self):
        self.assertEqual([], gates.source_violations(self.source))
        mutated = self.mutated(None, """
            leak2:
              runs-on: ubuntu-latest
              steps:
                - run: echo ${{ secrets.ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY }}
        """)
        self.assertTrue(gates.source_violations(mutated))

    NUGET_GUARD_RUN_BLOCK = (
        "        run: |\n"
        "          python3 scripts/stage_consolidated_candidate_nupkgs.py stage\n"
        "          python3 scripts/validate_packages_id_set.py\n"
    )
    NUGET_GUARD_RUN_VIOLATION = "publish_nuget candidate staging run: must be exactly stage and the ID-set guard"

    def test_nuget_guard_rejects_or_true(self):
        self.assert_rejected(
            self.mutated(
                self.NUGET_GUARD_RUN_BLOCK,
                self.NUGET_GUARD_RUN_BLOCK.replace(
                    "python3 scripts/validate_packages_id_set.py\n",
                    "python3 scripts/validate_packages_id_set.py || true\n",
                ),
            ),
            self.NUGET_GUARD_RUN_VIOLATION,
        )

    def test_nuget_guard_rejects_set_plus_e(self):
        self.assert_rejected(
            self.mutated(
                self.NUGET_GUARD_RUN_BLOCK,
                "        run: |\n"
                "          set +e\n"
                "          python3 scripts/stage_consolidated_candidate_nupkgs.py stage\n"
                "          python3 scripts/validate_packages_id_set.py\n",
            ),
            self.NUGET_GUARD_RUN_VIOLATION,
        )

    def test_nuget_guard_rejects_folded_run(self):
        self.assert_rejected(
            self.mutated(self.NUGET_GUARD_RUN_BLOCK, self.NUGET_GUARD_RUN_BLOCK.replace("run: |", "run: >-", 1)),
            self.NUGET_GUARD_RUN_VIOLATION,
        )

    def test_nuget_guard_rejects_shell(self):
        self.assert_rejected(
            self.mutated(
                "      - name: Stage and guard verified candidate packages\n",
                "      - name: Stage and guard verified candidate packages\n        shell: true {0}\n",
            ),
            "publish_nuget candidate staging keys must be among name, env, run, id, if, continue-on-error",
        )

    def test_nuget_guard_rejects_workflow_defaults_shell(self):
        self.assert_rejected(
            self.mutated("\njobs:\n", "\ndefaults:\n  run:\n    shell: bash\n\njobs:\n"),
            "the workflow must not set defaults.run.shell",
        )

    def test_nuget_guard_rejects_job_defaults_shell(self):
        self.assert_rejected(
            self.mutated(
                "    name: Publish release to nuget.org\n    needs: [build, validate_publication_selection]\n",
                "    name: Publish release to nuget.org\n    defaults:\n      run:\n        shell: bash\n    needs: [build, validate_publication_selection]\n",
            ),
            "publish_nuget must not set defaults.run.shell",
        )

    def test_real_packages_yml_guard_run_is_exact_lines(self):
        workflow = yaml.safe_load(self.source)
        step = next(
            step
            for step in workflow["jobs"]["publish_nuget"]["steps"]
            if isinstance(step, dict) and step.get("name") == "Stage and guard verified candidate packages"
        )
        self.assertEqual(list(gates.NUGET_GUARD_LINES), gates._run_lines(step))
        self.assertEqual([], gates.source_violations(self.source))

    def test_build_may_omit_node_setup(self):
        source = self.mutated(
            "      - name: Set up Node for React Studio assets\n"
            "        if: ${{ !(github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')) }}\n"
            "        uses: actions/setup-node@49933ea5288caeca8642d1e84afbd3f7d6820020 # v4.4.0\n"
            "        with:\n"
            "          node-version: '22'\n\n",
            "",
        )
        self.assertEqual([], gates.source_violations(source))

    def test_inverted_compile_pack_condition_is_rejected(self):
        source = self.mutated(
            "      - name: Compile+Pack\n"
            "        if: ${{ !((github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')) || ((github.event_name == 'release' || github.event_name == 'workflow_dispatch') && github.ref == 'refs/tags/3.10.0')) }}\n"
            "        run: ./build.sh Compile+Pack --version \"${VERSION}\" --analyseCode true\n",
            "      - name: Compile+Pack\n"
            "        if: ${{ github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-') }}\n"
            "        run: ./build.sh Compile+Pack --version \"${VERSION}\" --analyseCode true\n",
        )
        self.assert_rejected(source, "build Compile+Pack must skip a stable 3.10.x nuget.org promotion and the 3.10.0 candidate pack")

    def test_duplicate_compile_pack_is_rejected(self):
        source = self.mutated(
            "      - name: Compile+Pack\n"
            "        if: ${{ !((github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')) || ((github.event_name == 'release' || github.event_name == 'workflow_dispatch') && github.ref == 'refs/tags/3.10.0')) }}\n"
            "        run: ./build.sh Compile+Pack --version \"${VERSION}\" --analyseCode true\n",
            "      - name: Compile+Pack\n"
            "        if: ${{ !((github.event_name == 'workflow_dispatch' && inputs.publish_nuget && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-')) || ((github.event_name == 'release' || github.event_name == 'workflow_dispatch') && github.ref == 'refs/tags/3.10.0')) }}\n"
            "        run: ./build.sh Compile+Pack --version \"${VERSION}\" --analyseCode true\n"
            "\n"
            "      - name: Compile+Pack\n"
            "        run: ./build.sh Compile+Pack --version \"${VERSION}\" --analyseCode true\n",
        )
        self.assert_rejected(source, "build job must contain exactly one Compile+Pack step")

    def test_duplicate_download_packages_is_rejected(self):
        source = self.mutated(
            "      - name: NuGet login (OIDC)\n",
            "      - name: Download Packages\n"
            "        uses: actions/download-artifact@v4\n\n"
            "      - name: NuGet login (OIDC)\n",
        )
        self.assert_rejected(source, "publish_nuget must contain exactly one Download Packages step")

    def test_staging_condition_prefixed_with_false_is_rejected(self):
        source = self.mutated(
            "      - name: Stage and guard verified candidate packages\n"
            "        if: ${{ startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-') }}\n",
            "      - name: Stage and guard verified candidate packages\n"
            "        if: ${{ false && startsWith(github.ref, 'refs/tags/3.10.') && !contains(github.ref_name, '-') }}\n",
        )
        self.assert_rejected(source, "publish_nuget candidate staging if: must be exactly the stable 3.10.x tag condition")

    def test_real_workflow_uses_the_pinned_promote_conditions(self):
        self.assertEqual(gates.SKIP_STABLE_310_PACK, if_expression_of_real("Compile+Pack"))
        self.assertEqual(gates.CANDIDATE_PACK, if_expression_of_real("Pack consolidated 3.10.0 set"))
        self.assertEqual(gates.PROMOTE_STABLE_310, if_expression_of_real("Stage and guard verified candidate packages"))
        self.assertEqual(gates.PROMOTE_STABLE_310, if_expression_of_real("Fail closed on stable 3.10.x"))
        self.assertEqual([], gates.source_violations(self.source))

    def test_candidate_pack_must_run_the_id_set_guard(self):
        self.assert_rejected(
            self.mutated(
                "          python3 scripts/integration-program/prepare_consolidated_release_candidate.py --version 3.10.0 --output \"$RUNNER_TEMP/consolidated-candidate\" --remote-sources\n"
                "          python3 scripts/validate_packages_id_set.py\n",
                "          python3 scripts/integration-program/prepare_consolidated_release_candidate.py --version 3.10.0 --output \"$RUNNER_TEMP/consolidated-candidate\" --remote-sources\n"
                "          true\n",
            ),
            "build candidate pack run: must be exactly prepare, then the ID-set guard",
        )

    def test_npm_job_must_fail_closed_on_stable_310(self):
        self.assert_rejected(
            self.mutated(
                "      - name: Fail closed on stable 3.10.x\n",
                "      - name: Skip closed on stable 3.10.x\n",
            ),
            "publish_npm must fail closed on a stable 3.10.x tag",
        )

    def test_build_npm_must_pin_setup_dotnet(self):
        self.assert_rejected(
            self.mutated(
                "      - uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1\n        with:\n          dotnet-version: '10.0.300'\n",
                "      - uses: actions/setup-dotnet@v4\n        with:\n          dotnet-version: 10.x\n",
            ),
            "build_npm must pin setup-dotnet to the studio-npm-pair SHA",
        )

    def test_build_npm_must_need_build(self):
        self.assert_rejected(
            self.mutated(
                "  build_npm:\n    name: Build Studio npm pair\n    needs: [build, validate_publication_selection]\n",
                "  build_npm:\n    name: Build Studio npm pair\n    needs: [validate_publication_selection]\n",
            ),
            "build_npm must need build",
        )

    def test_build_npm_must_set_pythondontwritebytecode(self):
        self.assert_rejected(
            self.mutated(
                "    env:\n      PYTHONDONTWRITEBYTECODE: '1'\n",
                "",
            ),
            "build_npm must set PYTHONDONTWRITEBYTECODE to 1",
        )

    def test_build_npm_must_pin_sdk_version(self):
        self.assert_rejected(
            self.mutated(
                "          dotnet-version: '10.0.300'\n",
                "          dotnet-version: 10.x\n",
            ),
            "build_npm must pin setup-dotnet to 10.0.300",
        )

    def test_publish_npm_must_not_build_the_pair(self):
        self.assert_rejected(
            self.mutated(
                "      - name: Verify Studio npm pair\n",
                "      - name: Build the Studio npm pair\n        run: python3 scripts/integration-program/prove_studio_npm_pair.py\n\n      - name: Verify Studio npm pair\n",
            ),
            "publish_npm must not build the Studio pair; build_npm owns that step",
        )

    def test_feedz_publish_must_pass_a_dist_tag(self):
        self.assert_rejected(
            self.mutated(
                '            if output=$(npm publish "$file" --access public --tag "$tag" 2>&1); then\n',
                '            if output=$(npm publish "$file" --access public 2>&1); then\n',
            ),
            "publish_npm Feedz publish must pass a dist-tag",
        )

    def test_id_set_count_is_pinned(self):
        self.assertEqual([], gates.id_set_count_violations())
        self.assertEqual(231, gates.EXPECTED_PACKAGE_COUNT)


def if_expression_of_real(name: str) -> str:
    workflow = yaml.safe_load(gates.WORKFLOW.read_text(encoding="utf-8"))
    for job in workflow["jobs"].values():
        if not isinstance(job, dict):
            continue
        for step in job.get("steps") or []:
            if isinstance(step, dict) and step.get("name") == name:
                return gates.if_expression(step.get("if"))
    raise AssertionError(f"step {name!r} not found")


if __name__ == "__main__":
    unittest.main()

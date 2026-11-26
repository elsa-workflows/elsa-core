import json
import unittest

from select_pr_suites import ROUTES, pattern_regex, select_suites


class SelectPrSuitesTests(unittest.TestCase):
    def setUp(self):
        self.routes = json.loads(ROUTES.read_bytes())

    def selected(self, *paths, event="pull_request", branch="main", suite=None):
        return {
            key for key, enabled in select_suites(self.routes, event, branch, paths, suite).items()
            if enabled
        }

    def test_globs_cover_root_and_nested_files_with_case_classes(self):
        nuget_config = pattern_regex("**/[Nn]u[Gg]et.[Cc]onfig")
        self.assertIsNotNone(nuget_config.fullmatch("NuGet.Config"))
        self.assertIsNotNone(nuget_config.fullmatch("core/custom/nuget.config"))
        self.assertIsNone(pattern_regex("Directory.*").fullmatch("Directory.Build/props"))

    def test_docs_only_pr_selects_adr_check_and_skips_compile(self):
        self.assertEqual({"adr-index"}, self.selected("docs/adr/toc.md"))

    def test_docs_only_pr_elsewhere_stays_lightweight(self):
        self.assertEqual(set(), self.selected("core/docs/README.md", "README.md"))

    def test_paths_are_independent_across_renames_and_products(self):
        suites = self.selected("apps/old.csproj", "templates/new.csproj", ".github/workflows/packages.yml")
        self.assertTrue({"apps", "templates", "package-gates", "compile"} <= suites)

    def test_release_pr_preserves_non_main_jobs_and_excludes_main_only_smoke(self):
        self.assertTrue({"compile", "react-studio"} <= self.selected("docker/ElsaServer.Dockerfile", branch="release/3.11.0"))
        self.assertNotIn("docker-smoke", self.selected("docker/ElsaServer.Dockerfile", branch="release/3.11.0"))
        self.assertNotIn("compile", self.selected("Elsa.sln", branch="integration"))

    def test_push_main_only_selects_applicable_post_merge_suites(self):
        self.assertEqual({"docker-smoke"}, self.selected("docker/ElsaServer.Dockerfile", event="push"))
        self.assertEqual({"package-gates"}, self.selected(".github/workflows/packages.yml", event="push"))

    def test_workflow_edit_runs_every_eligible_suite(self):
        self.assertEqual(set(self.routes), self.selected(".github/workflows/pr.yml"))
        self.assertEqual({"docker-smoke", "package-gates"}, self.selected("scripts/ci/pr-paths.json", event="push"))

    def test_unavailable_push_base_selects_both_suites(self):
        self.assertEqual({"docker-smoke", "package-gates"}, {
            key for key, enabled in select_suites(self.routes, "push", "main", None).items() if enabled
        })

    def test_manual_run_selects_only_requested_suite(self):
        self.assertEqual({"persisted-upgrade"}, self.selected(event="workflow_dispatch", suite="persisted-upgrade"))
        with self.assertRaisesRegex(ValueError, "Unknown manual CI suite"):
            self.selected(event="workflow_dispatch", suite="adr-index")

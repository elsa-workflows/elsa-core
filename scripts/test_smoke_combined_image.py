#!/usr/bin/env python3
import unittest

from smoke_combined_image import collect_failures, role_names


def passing_result(*, environment: str = "Production", **overrides):
    result = {
        "environment": environment,
        "checks": {
            "routing": {
                "loginStatus": 200,
                "loginHasStudioChrome": True,
                "dashboardStatus": 200,
                "dashboardUsesHostFallback": True,
                "dashboardIsBlazorNotFound": False,
                "rootIsBlazorNotFound": False,
                "workflowsDefinitionsStatus": 200,
                "workflowsDefinitionsUsesHostFallback": True,
                "workflowsInstancesStatus": 200,
                "workflowsInstancesUsesHostFallback": True,
                "rootStatus": 200,
                "rootIsNotHealthText": True,
            },
            "health": {"live": 200, "ready": 200},
            "assets": {
                "designerCssStatus": 200,
                "designerCssBytes": 32,
                "designerEntryStatus": 200,
                "designerEntryBytes": 32,
                "blazorServerStatus": 200,
                "compressionRequestStatus": 200,
                "contentEncoding": "gzip",
            },
            "api": {
                "saveStatus": 201,
                "editStatus": 201,
                "publishStatus": 200,
                "published": True,
                "executeStatus": 200,
                "instanceId": "instance-1",
                "loadStatus": 200,
                "loadedName": "Combined Image WriteLine Edited",
                "instanceStatus": 200,
                "instanceWorkflowStatus": "Finished",
                "instanceSubStatus": "Finished",
            },
            "identity": {
                "createStatus": 200,
                "roleId": "role-1",
                "listStatus": 200,
                "rolePresent": True,
                "expectedRoleMissing": False,
            },
            "httpTriggers": {
                "startStatus": 200,
                "startBody": "started",
                "resumeStatus": 200,
                "resumeBody": "resumed",
                "reservedInstancesStartStatus": 200,
                "reservedInstancesStartBody": "started",
                "reservedInstancesResumeStatus": 200,
                "reservedInstancesResumeBody": "resumed",
                "reservedDefinitionsStartStatus": 200,
                "reservedDefinitionsStartBody": "started",
                "reservedDefinitionsResumeStatus": 200,
                "reservedDefinitionsResumeBody": "resumed",
                "unmatchedStatus": 404,
                "unmatchedServedStudio": False,
            },
            "upgrade": {"expectedWorkflowMissing": False, "expectedDefinitionMissing": False},
        }
    }
    for section, values in overrides.items():
        result["checks"][section].update(values)
    return result


class CombinedImageSmokeFailClosedTests(unittest.TestCase):
    def test_passing_result_has_no_failures(self):
        self.assertEqual(collect_failures(passing_result()), [])

    def test_ready_503_fails_closed(self):
        failures = collect_failures(passing_result(health={"ready": 503}))
        self.assertTrue(any("ready" in failure for failure in failures))

    def test_compressed_js_500_fails_closed(self):
        failures = collect_failures(passing_result(assets={"compressionRequestStatus": 500}))
        self.assertTrue(any("compressed" in failure for failure in failures))

    def test_faulted_instance_fails_closed(self):
        failures = collect_failures(passing_result(api={"instanceWorkflowStatus": "Faulted", "instanceSubStatus": "Faulted"}))
        self.assertTrue(any("Faulted" in failure for failure in failures))

    def test_dashboard_blazor_not_found_fails_closed(self):
        failures = collect_failures(passing_result(routing={"dashboardIsBlazorNotFound": True}))
        self.assertTrue(any("not-found" in failure for failure in failures))

    def test_http_start_regression_fails_closed(self):
        failures = collect_failures(passing_result(httpTriggers={"startStatus": 200, "startBody": html_studio()}))
        self.assertTrue(any("HTTP trigger start" in failure for failure in failures))

    def test_unmatched_http_serving_studio_fails_closed(self):
        failures = collect_failures(passing_result(httpTriggers={"unmatchedStatus": 200, "unmatchedServedStudio": True}))
        self.assertTrue(any("unmatched HTTP" in failure for failure in failures))

    def test_missing_role_after_restart_fails_closed(self):
        failures = collect_failures(passing_result(identity={"expectedRoleMissing": True}))
        self.assertTrue(any("identity role missing after restart" in failure for failure in failures))

    def test_missing_compression_encoding_fails_in_production(self):
        failures = collect_failures(passing_result(assets={"contentEncoding": ""}))
        self.assertTrue(any("missing content encoding" in failure for failure in failures))

    def test_missing_compression_encoding_allowed_in_development(self):
        failures = collect_failures(passing_result(environment="Development", assets={"contentEncoding": ""}))
        self.assertEqual(failures, [])

    def test_root_http_500_fails_closed(self):
        failures = collect_failures(passing_result(routing={"rootStatus": 500}))
        self.assertTrue(any("GET / did not return HTTP 200" in failure for failure in failures))

    def test_absent_terminal_status_fails_closed(self):
        failures = collect_failures(passing_result(api={"instanceWorkflowStatus": None, "instanceSubStatus": None}))
        self.assertTrue(any("Finished terminal status" in failure for failure in failures))

    def test_empty_terminal_status_fails_closed(self):
        failures = collect_failures(passing_result(api={"instanceWorkflowStatus": "", "instanceSubStatus": ""}))
        self.assertTrue(any("Finished terminal status" in failure for failure in failures))

    def test_reserved_prefix_http_start_regression_fails_closed(self):
        failures = collect_failures(passing_result(httpTriggers={"reservedInstancesStartStatus": 400, "reservedInstancesStartBody": ""}))
        self.assertTrue(any("/workflows/instances/{path}" in failure for failure in failures))

    def test_role_names_read_list_payload(self):
        names = role_names({"roles": [{"id": "1", "name": "admin"}, {"id": "2", "name": "qa-persist-role"}]})
        self.assertEqual(names, ["admin", "qa-persist-role"])


def html_studio() -> str:
    return "<html>designer.css blazor.server.js</html>"


if __name__ == "__main__":
    unittest.main()

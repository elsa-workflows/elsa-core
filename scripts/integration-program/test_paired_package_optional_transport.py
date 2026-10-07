"""Private transport contracts; synthetic envelopes are not native evidence."""
import json
from types import SimpleNamespace
import unittest
from unittest.mock import patch

from materialize_paired_package_hosts import CellRequest, RuntimeHandle
from paired_package_optional_features import optional_feature_profile
import paired_package_optional_transport as transport


def request_for(scenario):
    profile = optional_feature_profile(scenario)
    return CellRequest("wasm", "net8.0", "3.10.0",
                       backend_features=tuple(profile["backend_features"]),
                       permission_profile=profile["permission_profile"])


def envelope():
    return {"schema": 1, "phase": "optional-feature-probe",
            "cell": {"version": "3.10.0", "framework": "net8.0", "host": "wasm"},
            "probe": {}, "browser_version": "140.0.1.2", "cleanup_verified": True,
            "browser_alive_after_stop": None, "failure_category": None}


class OptionalTransportContracts(unittest.TestCase):
    def test_envelope_checks_identity_cleanup_and_bounded_fields(self):
        request = request_for("deny-secrets")
        value = envelope()
        self.assertEqual(value, transport.validate_envelope(value, request, "deny-secrets"))
        for update in ({"schema": True}, {"phase": "main"}, {"cell": {}}, {"probe": []},
                       {"probe": None}, {"browser_version": "raw private string"},
                       {"browser_version": None}, {"cleanup_verified": False},
                       {"cleanup_verified": 1}, {"browser_alive_after_stop": True},
                       {"failure_category": "raw private error"}, {"unexpected": True}):
            with self.subTest(update=update), self.assertRaises(ValueError):
                transport.validate_envelope(dict(value, **update), request, "deny-secrets")

    def test_failed_envelope_cannot_imply_cleanup_or_native_acceptance(self):
        value = dict(envelope(), probe=None, browser_version=None, cleanup_verified=False,
                     failure_category="optional_probe_execution_failed")
        self.assertEqual(value, transport.validate_envelope(value, request_for("deny-secrets"), "deny-secrets"))
        for alive in (None, True, False):
            transport.validate_envelope(dict(envelope(), browser_alive_after_stop=alive),
                                        request_for("disconnect"), "disconnect")
        for alive in (1, "true", []):
            with self.subTest(alive=alive), self.assertRaises(ValueError):
                transport.validate_envelope(dict(envelope(), browser_alive_after_stop=alive),
                                            request_for("disconnect"), "disconnect")

    def test_private_input_and_inherited_fd_are_used_without_npm(self):
        request = request_for("deny-secrets")
        handle = RuntimeHandle(request, "http://127.0.0.1:3010", "http://127.0.0.1:3011/elsa/api",
                               "private-user", "private-password")
        captured = {}
        def child(command, **options):
            captured.update(command=command, **options)
            control = options["control"].__self__
            control.completed_events.extend(control.events)
            control.complete = True
            return SimpleNamespace(stdout=json.dumps(envelope()), returncode=0)
        with patch.object(transport.browser, "_run_browser_process", side_effect=child):
            self.assertEqual(envelope(), transport.run_optional_browser(handle, request, "deny-secrets", on_event=lambda _: None))
        self.assertEqual(["node", "--import", "tsx"], captured["command"][:3])
        payload = json.loads(captured["input"])
        self.assertEqual("private-password", payload["password"])
        self.assertEqual((payload["optional_probe"]["control"]["fd"],), captured["pass_fds"])
        self.assertNotIn("private-password", str(captured["command"]) + str(captured["env"]))

    def test_unacknowledged_control_cannot_return_success(self):
        request = request_for("deny-secrets")
        handle = RuntimeHandle(request, "http://127.0.0.1:3010", "http://127.0.0.1:3011/elsa/api", "u", "p")
        with patch.object(transport.browser, "_run_browser_process", return_value=SimpleNamespace(stdout=json.dumps(envelope()), returncode=0)):
            with self.assertRaisesRegex(ValueError, "control did not complete"):
                transport.run_optional_browser(handle, request, "deny-secrets", on_event=lambda _: None)


if __name__ == "__main__":
    unittest.main()

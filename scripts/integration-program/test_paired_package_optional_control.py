"""Real private socket contracts; no host, Node or browser process is launched."""
import json
from types import SimpleNamespace
import unittest

from paired_package_optional_control import OptionalProbeControl


class OptionalControlContracts(unittest.TestCase):
    def control(self, scenario="without-secrets", callback=None):
        self.events = []
        control = OptionalProbeControl(scenario, callback or self.events.append, timeout=2)
        # Model the inherited descriptor independently of the parent's duplicate.
        peer = control.child.dup()
        peer.settimeout(2)
        self.addCleanup(peer.close)
        self.addCleanup(control.close)
        return control, peer

    def send(self, peer, control, event, seq=1, **overrides):
        message = {"schema": 1, "nonce": control.nonce, "seq": seq, "event": event} | overrides
        peer.sendall((json.dumps(message) + "\n").encode())
        return message

    def ack(self, peer):
        raw = bytearray()
        while not raw.endswith(b"\n"):
            value = peer.recv(1)
            if not value:
                return None
            raw.extend(value)
        return json.loads(raw)

    def test_action_ack_follows_parent_observation_and_peer_close(self):
        control, peer = self.control()
        descriptor = control.descriptor()
        self.assertGreaterEqual(descriptor["fd"], 3)
        self.assertEqual(64, len(descriptor["nonce"]))
        with control.session(SimpleNamespace(poll=lambda: None)):
            expected = self.send(peer, control, "begin-native-action")
            self.assertEqual(expected | {"ack": True}, self.ack(peer))
            self.assertEqual(["begin-native-action"], self.events)
            peer.close()
        self.assertTrue(control.complete)
        with self.assertRaises(RuntimeError):
            control.descriptor()
        with self.assertRaises(RuntimeError), control.session(SimpleNamespace(poll=lambda: None)):
            pass

    def test_disconnect_ack_precedes_action_and_both_callbacks_finish(self):
        control, peer = self.control("disconnect")
        with control.session(SimpleNamespace(poll=lambda: None)):
            for index, event in enumerate(control.events, 1):
                expected = self.send(peer, control, event, index)
                self.assertEqual(expected | {"ack": True}, self.ack(peer))
                self.assertEqual(list(control.events[:index]), self.events)
            peer.close()
        self.assertEqual(list(control.events), control.completed_events)

    def test_wrong_nonce_sequence_event_extra_fields_or_boolean_sequence_never_ack(self):
        for change in ({"nonce": "0" * 64}, {"seq": 2}, {"seq": True}, {"schema": True},
                       {"event": "disconnect-ready"}, {"raw": "private"}):
            with self.subTest(change=change):
                control, peer = self.control()
                with self.assertRaises(RuntimeError), control.session(SimpleNamespace(poll=lambda: None)):
                    message = {"schema": 1, "nonce": control.nonce, "seq": 1, "event": "begin-native-action"} | change
                    peer.sendall((json.dumps(message) + "\n").encode())
                    self.assertIsNone(self.ack(peer))
                    peer.close()
                self.assertEqual([], self.events)

    def test_duplicate_fields_oversize_partial_and_early_eof_fail_closed(self):
        for raw in (b'{"schema":1,"schema":1}\n', b'x' * 513, b'{"schema":', b''):
            with self.subTest(size=len(raw)):
                control, peer = self.control()
                with self.assertRaises(RuntimeError), control.session(SimpleNamespace(poll=lambda: None)):
                    peer.sendall(raw)
                    peer.close()
                self.assertFalse(control.complete)

    def test_skipped_disconnect_and_extra_exchange_fail(self):
        control, peer = self.control("disconnect")
        with self.assertRaises(RuntimeError), control.session(SimpleNamespace(poll=lambda: None)):
            self.send(peer, control, "begin-native-action")
            self.assertIsNone(self.ack(peer))
            peer.close()
        self.assertEqual([], self.events)
        control, peer = self.control()
        with self.assertRaises(RuntimeError), control.session(SimpleNamespace(poll=lambda: None)):
            self.send(peer, control, "begin-native-action")
            self.assertIsNotNone(self.ack(peer))
            self.send(peer, control, "begin-native-action", 2)
            peer.close()
        self.assertFalse(control.complete)

    def test_callback_failure_or_process_exit_cannot_acknowledge(self):
        for failure in ("callback", "process"):
            with self.subTest(failure=failure):
                alive = [True]
                def callback(_event):
                    if failure == "callback":
                        raise ValueError("synthetic private callback failure")
                    alive[0] = False
                control, peer = self.control(callback=callback)
                with self.assertRaisesRegex(RuntimeError, "not verified"), control.session(
                        SimpleNamespace(poll=lambda: None if alive[0] else 1)):
                    self.send(peer, control, "begin-native-action")
                    self.assertIsNone(self.ack(peer))
                    peer.close()
                self.assertEqual([], control.completed_events)


if __name__ == "__main__":
    unittest.main()

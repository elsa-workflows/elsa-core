"""Synthetic embedding contracts; no host/browser execution evidence."""
import copy
import hashlib
import unittest

import paired_package_embedding as embedding


def fixture(prefix=8, *, parent=True):
    checks = {name: index < prefix for index, name in enumerate(embedding.CHECKS)}
    value = {"checks": checks}
    for name, check in embedding.HASH_CHECKS.items():
        if checks[check]:
            value[name] = hashlib.sha256(name.encode()).hexdigest()
    assertions = {"authentication": prefix >= 2, "native_callbacks": prefix >= 6, "instance_list_viewer": prefix == 8}
    proof = {name: value[name] for name in embedding.PARENT_BINDINGS if name in value} if parent else {}
    return value, assertions, proof


def validate(value, assertions, proof, key=("3.10.0", "net10.0", "custom-elements")):
    embedding.validate_embedding(value, assertions, proof, key)


class EmbeddingReceiptContracts(unittest.TestCase):
    def test_complete_observations_cover_declared_versions_and_frameworks(self):
        for version in ("3.8.4", "3.9.0", "3.10.0"):
            for framework in ("net8.0", "net9.0", "net10.0"):
                with self.subTest(version=version, framework=framework):
                    value, assertions, proof = fixture()
                    original = copy.deepcopy((value, assertions, proof))
                    validate(value, assertions, proof, (version, framework, "custom-elements"))
                    self.assertEqual((value, assertions, proof), original)

    def test_each_partial_prefix_preserves_observations_before_parent_hashes(self):
        for prefix in range(6):
            with self.subTest(prefix=prefix):
                value, assertions, proof = fixture(prefix, parent=False)
                validate(value, assertions, proof)
                assertions["authentication"] = False
                validate(value, assertions, proof)

    def test_callback_success_and_later_instance_failure_keep_primary_bindings(self):
        for prefix in (6, 7, 8):
            with self.subTest(prefix=prefix):
                validate(*fixture(prefix))
                value, assertions, proof = fixture(prefix, parent=False)
                with self.assertRaisesRegex(ValueError, "lack parent"):
                    validate(value, assertions, proof)

    def test_each_present_parent_binding_rejects_forged_callback_identity_even_when_partial(self):
        for name, prefix in (("definition_id_sha256", 3), ("activity_id_sha256", 4), ("instance_id_sha256", 6)):
            with self.subTest(field=name):
                value, assertions, proof = fixture(prefix)
                proof[name] = "0" * 64
                with self.assertRaisesRegex(ValueError, "differs from parent"):
                    validate(value, assertions, proof)

    def test_each_required_success_parent_binding_must_exist(self):
        for name in embedding.PARENT_BINDINGS:
            with self.subTest(field=name):
                value, assertions, proof = fixture()
                del proof[name]
                with self.assertRaisesRegex(ValueError, "lack parent"):
                    validate(value, assertions, proof)

    def test_no_proof_cannot_support_authentication_or_callback_claims(self):
        assertions = dict.fromkeys(embedding.ASSERTIONS, False)
        validate(None, assertions, {})
        for name in embedding.ASSERTIONS:
            with self.subTest(assertion=name), self.assertRaisesRegex(ValueError, "Missing embedding proof"):
                validate(None, dict(assertions, **{name: True}), {})

    def test_embedding_is_forbidden_in_other_hosts(self):
        for host in ("server", "wasm", "hosted-wasm"):
            with self.subTest(host=host):
                key = ("3.10.0", "net10.0", host)
                validate(None, {"authentication": True}, {}, key)
                with self.assertRaisesRegex(ValueError, "non-CustomElements"):
                    validate(*fixture(), key=key)
                with self.assertRaisesRegex(ValueError, "Unexpected embedding assertion"):
                    validate(None, {"native_callbacks": True}, {}, key)

    def test_checks_require_ordered_native_authentication_and_callback_predecessors(self):
        for index, name in enumerate(embedding.CHECKS[1:], 1):
            with self.subTest(check=name):
                value, assertions, proof = fixture(index + 1)
                value["checks"][embedding.CHECKS[index - 1]] = False
                with self.assertRaisesRegex(ValueError, "missing predecessors"):
                    validate(value, assertions, proof)

    def test_hash_presence_exactly_matches_related_observation(self):
        for name, check in embedding.HASH_CHECKS.items():
            with self.subTest(field=name, missing=True):
                value, assertions, proof = fixture()
                del value[name]
                with self.assertRaisesRegex(ValueError, "differs from observed callback"):
                    validate(value, assertions, proof)
            with self.subTest(field=name, unobserved=True):
                value, assertions, proof = fixture(embedding.CHECKS.index(check))
                value[name] = "a" * 64
                with self.assertRaisesRegex(ValueError, "differs from observed callback"):
                    validate(value, assertions, proof)

    def test_passed_claims_and_complete_observations_must_agree_both_directions(self):
        for prefix, assertion in ((5, "native_callbacks"), (7, "instance_list_viewer"),
                                  (6, "native_callbacks"), (8, "instance_list_viewer")):
            with self.subTest(prefix=prefix, assertion=assertion):
                value, assertions, proof = fixture(prefix)
                assertions[assertion] = not assertions[assertion]
                with self.assertRaisesRegex(ValueError, "assertion differs"):
                    validate(value, assertions, proof)

    def test_authentication_claim_requires_native_request_and_backend_configuration(self):
        for prefix in (0, 1):
            with self.subTest(prefix=prefix):
                value, assertions, proof = fixture(prefix)
                assertions["authentication"] = True
                with self.assertRaisesRegex(ValueError, "lacks native"):
                    validate(value, assertions, proof)

    def test_unsafe_fields_types_and_noncanonical_hashes_fail_closed(self):
        mutations = [
            lambda value: value.update(token="private"), lambda value: value.update(url="http://127.0.0.1"),
            lambda value: value.update(raw_id="abcdef"), lambda value: value.pop("checks"),
            lambda value: value["checks"].pop("instance_viewer"), lambda value: value["checks"].update(extra=False),
            lambda value: value["checks"].update(native_authentication=1),
            lambda value: value.update(version_id_sha256="A" * 64),
            lambda value: value.update(definition_id_sha256="g" * 64),
            lambda value: value.update(activity_id_sha256="a" * 63),
            lambda value: value.update(instance_id_sha256=None),
        ]
        for index, mutate in enumerate(mutations):
            with self.subTest(mutation=index), self.assertRaises(ValueError):
                value, assertions, proof = fixture()
                mutate(value)
                validate(value, assertions, proof)
        for name in embedding.ASSERTIONS:
            with self.subTest(assertion=name), self.assertRaisesRegex(ValueError, "Invalid embedding assertions"):
                value, assertions, proof = fixture()
                assertions[name] = 1
                validate(value, assertions, proof)


if __name__ == "__main__":
    unittest.main()

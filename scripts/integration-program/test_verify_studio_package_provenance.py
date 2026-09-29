"""Regression tests for the Studio package SourceLink commit gate."""

import json
import unittest

from verify_studio_package_provenance import verify_source_link_documents


class StudioPackageProvenanceTests(unittest.TestCase):
    def test_source_link_documents_must_resolve_to_exact_core_commit(self) -> None:
        commit = "a" * 40
        expected = f"https://raw.githubusercontent.com/elsa-workflows/elsa-core/{commit}/*"
        verify_source_link_documents(json.dumps({"documents": {"/_/*": expected}}), commit)

        for documents in ({}, {"/_/*": expected.replace(commit, "b" * 40)},
                          {"/_/*": expected.replace("elsa-core", "elsa-studio")},
                          {"/_/*": expected, "/other/*": "https://example.com/*"}):
            with self.subTest(documents=documents), self.assertRaises(ValueError):
                verify_source_link_documents(json.dumps({"documents": documents}), commit)

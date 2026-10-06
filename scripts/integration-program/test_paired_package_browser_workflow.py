from pathlib import Path
import re
import unittest

import consolidated_candidate_input as candidate


class BrowserWorkflowBoundaryTests(unittest.TestCase):
    def test_original_input_reader_cannot_execute_checkout_code(self):
        text = (Path(__file__).resolve().parents[2] / ".github/workflows/paired-package-browser-proof.yml").read_text()
        retrieve, proof = text.split("\n  proof:\n", 1)
        reader = retrieve.split("\n  retrieve:\n", 1)[1]
        # Parse the deliberately restricted permission maps and reject extra keys,
        # aliases, scalar shortcuts, or duplicate permission blocks.
        def permissions(block):
            self.assertEqual(len(re.findall(r"^    permissions:", block, re.M)), 1)
            matches = re.findall(r"^    permissions:\n((?:      [^\n]+\n)+)", block, re.M)
            self.assertEqual(len(matches), 1)
            pairs = [line.strip().split(": ", 1) for line in matches[0].splitlines()]
            self.assertTrue(all(len(pair) == 2 for pair in pairs))
            self.assertEqual(len({key for key, _ in pairs}), len(pairs))
            return dict(pairs)
        self.assertEqual(len(re.findall(r"^permissions:", text, re.M)), 1)
        self.assertEqual(re.findall(r"^permissions: (.*)$", text, re.M), ["{}"])
        self.assertEqual(permissions(reader), {"actions": "read"})
        self.assertEqual(permissions(proof), {"contents": "read", "actions": "none"})
        self.assertNotIn("pull_request", text)
        self.assertIn("workflow_dispatch:", text)
        for forbidden in ("actions/checkout", "scripts/", "python", "node", "secrets."):
            self.assertNotIn(forbidden, reader)
        for pin in (str(candidate.ARTIFACT), str(candidate.RUN), candidate.SOURCE, candidate.ARCHIVE_SHA256):
            self.assertIn(pin, reader)
        self.assertIn("/attempts/1", reader)
        self.assertIn(".run_attempt == 1", reader)
        self.assertIn("sha256sum --check --status", reader)
        for name in ("candidate.zip", "artifact.json", "producer-run.json", "live-retrieval.json"):
            self.assertIn("${{ runner.temp }}/browser-input/" + name, reader)
        self.assertIn("retention-days: 1", reader)
        self.assertIn("actions: none", proof)
        self.assertIn("artifact-ids: ${{ needs.retrieve.outputs.input-artifact-id }}", proof)
        self.assertIn("merge-multiple: true", proof)
        self.assertIn("ref: ${{ github.sha }}", proof)
        self.assertIn("persist-credentials: false", proof)
        # Python transport contracts execute the locked Node parser contracts.
        self.assertLess(proof.index("npm ci --ignore-scripts"), proof.index("python3 -m unittest"))
        self.assertIn("paired-package-browser-proof/retained-evidence/matrix.json", proof)
        self.assertIn("check_matrix(ledger)", proof)
        self.assertIn('ledger.get("development_only") is not False', proof)
        for forbidden in ("github-token:", "GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN", "run-id:", "repository:", "--cell"):
            self.assertNotIn(forbidden, proof)
        for argument in ('--fixture-source "${{ github.sha }}"', '--fixture-run "${{ github.run_id }}"',
                         '--fixture-attempt "${{ github.run_attempt }}"'):
            self.assertIn(argument, proof)
        upload = proof.split("- name: Retain only allowlisted browser evidence", 1)[1]
        self.assertIn("if: always() && steps.retained.outputs.validated == 'true'", upload)
        self.assertIn('verify_paired_browser_retention.py "$evidence"', proof)
        self.assertIn("path: ${{ runner.temp }}/paired-package-browser-proof/retained-evidence", upload)
        self.assertNotIn("**", upload)
        self.assertIn("retention-days: 7", upload)
        for action in re.findall(r"uses: ([^\s]+)", text):
            self.assertRegex(action, r"^[A-Za-z0-9_./-]+@[0-9a-f]{40}$")


if __name__ == "__main__":
    unittest.main()

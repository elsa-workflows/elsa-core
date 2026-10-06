from pathlib import Path
import re
import unittest

import consolidated_candidate_input as candidate


class BrowserWorkflowBoundaryTests(unittest.TestCase):
    def test_original_input_reader_cannot_execute_checkout_code(self):
        text = (Path(__file__).resolve().parents[2] / ".github/workflows/paired-package-browser-proof.yml").read_text()
        retrieve, proof = text.split("\n  proof:\n", 1)
        reader = retrieve.split("\n  retrieve:\n", 1)[1]
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
        for forbidden in ("github-token:", "GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN", "run-id:", "repository:", "--cell"):
            self.assertNotIn(forbidden, proof)
        for argument in ('--fixture-source "${{ github.sha }}"', '--fixture-run "${{ github.run_id }}"',
                         '--fixture-attempt "${{ github.run_attempt }}"'):
            self.assertIn(argument, proof)
        upload = proof.split("- name: Retain only allowlisted browser evidence", 1)[1]
        self.assertIn("if: always()", upload)
        self.assertIn("path: ${{ runner.temp }}/paired-package-browser-proof/retained-evidence", upload)
        self.assertNotIn("**", upload)
        self.assertIn("retention-days: 7", upload)
        for action in re.findall(r"uses: ([^\s]+)", text):
            self.assertRegex(action, r"^[A-Za-z0-9_./-]+@[0-9a-f]{40}$")


if __name__ == "__main__":
    unittest.main()

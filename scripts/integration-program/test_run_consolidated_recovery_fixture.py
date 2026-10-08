"""Recovery simulation retains original ZIP metadata and independent visibility."""
import io
from pathlib import Path
import tempfile
import unittest
import zipfile

import consolidated_package_recovery as recovery
import run_consolidated_recovery_fixture as fixture


class StreamingBuffer(io.BytesIO):
    def seek(self, *args):
        raise io.UnsupportedOperation("streaming ZIP output")


class RecoveryFixtureContracts(unittest.TestCase):
    def test_duplicate_remote_identity_does_not_mutate_source_zip_offsets(self):
        stream = StreamingBuffer()
        members = {"_rels/.rels": b"relationship before nuspec", "Elsa.Synthetic.nuspec": b"<package/>",
                   "lib/net8.0/Synthetic.dll": b"original payload"}
        with zipfile.ZipFile(stream, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for name, data in members.items():
                archive.writestr(name, data)
        original = stream.getvalue()
        changed = fixture._remote_mutation(original, duplicate=True)
        with zipfile.ZipFile(io.BytesIO(changed)) as archive:
            self.assertEqual(set(members) | {"ELSA.SYNTHETIC.NUSPEC"}, set(archive.namelist()))
            for name, expected in members.items():
                self.assertEqual(expected, archive.read(name))
        with zipfile.ZipFile(io.BytesIO(original)) as archive:
            self.assertEqual(set(members), set(archive.namelist()))
        with self.assertRaises(ValueError):
            recovery._parse_package(changed, recovery._verifier())

    def test_accepted_bytes_become_visible_without_a_second_acceptance(self):
        with tempfile.TemporaryDirectory() as directory:
            feed = fixture.SimulatedFeed(Path(directory))
            with self.assertRaises(fixture.SimulatedAcceptanceInterrupted):
                feed.simulate_accept("Elsa.Synthetic", b"original", visible=False, interrupt=True)
            url = feed.base + "elsa.synthetic/3.10.0/elsa.synthetic.3.10.0.nupkg"
            self.assertEqual(404, feed.get(url).status)
            feed.visible.add("elsa.synthetic")
            self.assertEqual(b"original", feed.get(url).body)
            self.assertEqual(1, feed.simulated_acceptance_calls)
            with self.assertRaises(ValueError):
                feed.simulate_accept("Elsa.Synthetic", b"duplicate")


if __name__ == "__main__":
    unittest.main()

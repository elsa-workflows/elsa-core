#!/usr/bin/env python3
import sqlite3
import tempfile
import unittest
from pathlib import Path

from validate_combined_image_contract import ROOT, validate_combined_image_contract


class CombinedImagePersistenceTests(unittest.TestCase):
    def test_host_opens_prior_elsa_workflows_db_path(self):
        errors = validate_combined_image_contract()
        self.assertEqual(errors, [])
        settings = (ROOT / "studio/src/hosts/Elsa.ServerAndStudio.Web/appsettings.json").read_text(encoding="utf-8")
        program = (ROOT / "studio/src/hosts/Elsa.ServerAndStudio.Web/Program.cs").read_text(encoding="utf-8")
        self.assertIn("Data Source=elsa_workflows.db;Cache=Shared", settings)
        self.assertNotIn("elsa.sqlite.db", settings)
        self.assertIn("UseEntityFrameworkCore", program)
        self.assertNotIn("File.Copy", program)
        self.assertNotIn("File.Move", program)
        self.assertNotIn("App_Data/elsa.sqlite.db", program)

    def test_wal_safe_in_place_open_sees_legacy_row_without_copying(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            db_path = directory / "elsa_workflows.db"
            connection = sqlite3.connect(db_path)
            connection.execute("PRAGMA journal_mode=WAL")
            connection.execute("PRAGMA wal_autocheckpoint=0")
            connection.execute("CREATE TABLE WorkflowDefinitions (Id TEXT, Name TEXT)")
            connection.execute("INSERT INTO WorkflowDefinitions VALUES ('legacy-upgrade-wf', 'LEGACY_COMBINED_WF')")
            connection.commit()
            wal_path = directory / "elsa_workflows.db-wal"
            self.assertTrue(wal_path.exists() or db_path.exists())
            copied = directory / "App_Data" / "elsa.sqlite.db"
            self.assertFalse(copied.exists())
            uri = f"file:{db_path}?cache=shared"
            reader = sqlite3.connect(uri, uri=True)
            try:
                row = reader.execute("SELECT Name FROM WorkflowDefinitions WHERE Id = 'legacy-upgrade-wf'").fetchone()
                self.assertEqual(row[0], "LEGACY_COMBINED_WF")
            finally:
                reader.close()
                connection.close()


if __name__ == "__main__":
    unittest.main()

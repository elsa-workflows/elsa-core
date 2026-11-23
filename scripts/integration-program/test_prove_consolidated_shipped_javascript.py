import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import shipped_javascript as shipped


class ShippedJavaScriptTests(unittest.TestCase):
    def test_packed_source_map_excludes_unused_inputs_and_resolves_nested_versions(self):
        with tempfile.TemporaryDirectory() as directory:
            client = Path(directory) / "ClientLib"
            client.mkdir()
            lock = {name: {"version": version} for name, version in [
                ("node_modules/react", "18.3.1"), ("node_modules/unused", "1"),
                ("node_modules/ui/node_modules/react", "17.0.2")]}
            (client / "package-lock.json").write_text(json.dumps({"packages": lock}))
            asset = "staticwebassets/editor.js"
            mapping = {"version": 3, "file": "editor.js", "sources": [
                "webpack://editor/./node_modules/react/index.js",
                "webpack://editor/./node_modules/ui/node_modules/react/index.js", "webpack://editor/src/index.ts"]}
            with zipfile.ZipFile(io.BytesIO(), "w") as z:
                z.writestr(asset, "bundled")
                z.writestr(asset + ".map", json.dumps(mapping))
                record = shipped.browser_components(z, [{"package_path": asset}], client, [client])[asset]
            self.assertEqual(record["components"], [("react", "17.0.2"), ("react", "18.3.1")])

    def test_clr_resources_must_match_named_locked_entrypoints_and_exact_bytes(self):
        with tempfile.TemporaryDirectory() as directory:
            client = Path(directory)
            (client / "dist").mkdir()
            (client / "src").mkdir()
            content = b"exports.version='4.18.1'"
            (client / "dist/lodash.js").write_bytes(content)
            (client / "src/lodash.js").write_text("module.exports = require('lodash');")
            (client / "package-lock.json").write_text(json.dumps({"packages": {"node_modules/lodash": {"version": "4.18.1"}}}))
            assembly = "lib/net10.0/Elsa.Expressions.JavaScript.Libraries.dll"
            symbol = "Elsa.Expressions.JavaScript.Libraries.ClientLib.dist.lodash.js"
            row = {"id":"Elsa.Expressions.JavaScript.Libraries", "assemblies":[{"assembly":assembly}]}
            for digest in (shipped.sha(content), shipped.sha(b"changed")):
                response = json.dumps({assembly:{symbol:digest}}).encode()
                with patch.object(shipped.subprocess, "check_output", return_value=response):
                    if digest != shipped.sha(content):
                        with self.assertRaisesRegex(ValueError, "Packed CLR resource differs"):
                            shipped.embedded_libraries(client / "p.nupkg", row, client)
                    else:
                        result = shipped.embedded_libraries(client / "p.nupkg", row, client)
                        self.assertEqual(result[assembly]["resources"][0], {
                            "name":symbol, "sha256":digest, "components":[["lodash", "4.18.1"]]})

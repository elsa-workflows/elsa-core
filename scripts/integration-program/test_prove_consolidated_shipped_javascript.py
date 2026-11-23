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
                ("node_modules/react", "18.3.1"), ("node_modules/unused", "1"), ("node_modules/webpack", "5.111.1"),
                ("node_modules/ui/node_modules/react", "17.0.2")]}
            (client / "package-lock.json").write_text(json.dumps({"packages": lock}))
            asset = "staticwebassets/editor.js"
            mapping = {"version": 3, "file": "editor.js", "sources": [
                "webpack://editor/./node_modules/react/index.js",
                "webpack://editor/./node_modules/ui/node_modules/react/index.js", "webpack://editor/src/index.ts",
                "webpack://editor/webpack/runtime/hasOwnProperty shorthand"]}
            with zipfile.ZipFile(io.BytesIO(), "w") as z:
                z.writestr(asset, "bundled")
                z.writestr(asset + ".map", json.dumps(mapping))
                record = shipped.browser_components(z, [{"package_path": asset}], client, [client])[asset]
            self.assertEqual(record["components"], [("react", "17.0.2"), ("react", "18.3.1"), ("webpack", "5.111.1")])

    def test_clr_resources_must_match_named_locked_entrypoints_and_exact_bytes(self):
        with tempfile.TemporaryDirectory() as directory:
            client = Path(directory)
            (client / "dist").mkdir()
            (client / "src").mkdir()
            content = b"r.nmd=n=>(n.paths=[],n);exports.version='4.18.1'"
            (client / "dist/lodash.js").write_bytes(content)
            (client / "src/lodash.js").write_text("module.exports = require('lodash');")
            (client / "package-lock.json").write_text(json.dumps({"packages": {"node_modules/lodash": {"version": "4.18.1"}, "node_modules/webpack": {"version": "5.107.2"}}}))
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
                            "name":symbol, "sha256":digest, "components":[["lodash", "4.18.1"], ["webpack", "5.107.2"]]})


    def test_vite_modules_must_match_the_same_emitted_and_embedded_asset(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            record = {"file": "dist/shared/react.js", "sha256": "a" * 64,
                      "components": [["react", "19.3.0"]], "imports": ["chunks/shared.js"]}
            (root / "react-vite-assets.jsonl").write_text(json.dumps(record))
            row = {"id": "Elsa.ReactStudio.Host"}
            name = "ReactStudio/shared/react.js"
            for digest in (record["sha256"], "b" * 64):
                with patch.object(shipped, "inspected_resources", return_value={"lib/Host.dll": {name: digest}}):
                    if digest == record["sha256"]:
                        resource = shipped.react_libraries(root / "Host.nupkg", row, root, root)["lib/Host.dll"]["resources"][0]
                        self.assertEqual(resource["components"], [["react", "19.3.0"]])
                    else:
                        with self.assertRaisesRegex(ValueError, "CLR resource differs"):
                            shipped.react_libraries(root / "Host.nupkg", row, root, root)

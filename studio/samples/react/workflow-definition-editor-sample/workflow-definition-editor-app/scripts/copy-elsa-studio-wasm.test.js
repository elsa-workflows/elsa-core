import {test} from "node:test";
import assert from "node:assert/strict";
import {execFileSync} from "node:child_process";
import {existsSync, readFileSync, writeFileSync} from "node:fs";

test("copies the Studio assets index.html references and drops stale ones", () => {
    const stale = "public/_framework/stale-from-an-older-package.js";
    execFileSync("node", ["scripts/copy-elsa-studio-wasm.js"]);
    writeFileSync(stale, "");

    execFileSync("node", ["scripts/copy-elsa-studio-wasm.js"]);

    assert.equal(existsSync(stale), false);
    const referenced = [...readFileSync("index.html", "utf8").matchAll(/(?:href|src)="\/((?:_content|_framework|Elsa\.)[^"]+)"/g)].map(match => match[1]);
    assert.ok(referenced.length > 0);
    for (const asset of referenced)
        assert.ok(existsSync(`public/${asset}`), `missing public/${asset}`);
});

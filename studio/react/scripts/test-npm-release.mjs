import assert from "node:assert/strict";
import { test } from "node:test";
import { cpSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { packageNames, publicationPlan, readBundle } from "./npm-release.mjs";

const bundle = version => ({ version, packages: packageNames.map((name, index) => ({ name, integrity: `sha512-${index}` })) });
test("previews use the preview dist-tag and stable publication requires previews for every package", () => {
  assert.ok(publicationPlan(bundle("3.11.0-preview.1"), [{}, {}, {}, {}]).every(item => item.tag === "preview" && !item.skip));
  const prior = () => ({ versions: { "3.11.0-preview.1": {} } });
  const metadata = packageNames.map(prior);
  assert.ok(publicationPlan(bundle("3.11.0"), metadata).every(item => item.tag === "latest"));
  metadata[3] = {};
  assert.throws(() => publicationPlan(bundle("3.11.0"), metadata), /Publish a 3.11.0 preview first/);
});
test("a partially published version can resume only when the registry bytes match", () => {
  const release = bundle("3.11.0-preview.2");
  const metadata = [{ versions: { [release.version]: { dist: { integrity: release.packages[0].integrity } } } }, {}, {}, {}];
  assert.deepEqual(publicationPlan(release, metadata).map(item => item.skip), [true, false, false, false]);
  metadata[0].versions[release.version].dist.integrity = "sha512-other";
  assert.throws(() => publicationPlan(release, metadata), /different bytes/);
});
test("publication cannot move either public dist-tag backwards", () => {
  assert.throws(() => publicationPlan(bundle("3.11.0-preview.2"), [{ "dist-tags": { preview: "3.11.0-preview.10" } }]), /backwards/);
  assert.throws(() => publicationPlan(bundle("3.11.0"), [{ versions: { "3.11.0-preview.1": {} }, "dist-tags": { latest: "3.12.0" } }]), /backwards/);
});

if (process.argv[2]) {
  test("a prepared artifact rejects changed tarball bytes and another source commit", () => {
    const directory = mkdtempSync(join(tmpdir(), "elsa-release-integrity-"));
    try {
      cpSync(process.argv[2], directory, { recursive: true });
      const release = readBundle(directory);
      assert.throws(() => readBundle(directory, "0".repeat(40)), /selected source commit/);
      const file = join(directory, release.packages[0].file);
      const data = readFileSync(file);
      data[0] ^= 1;
      writeFileSync(file, data);
      assert.throws(() => readBundle(directory), /Changed tarball/);
    } finally { rmSync(directory, { recursive: true, force: true }); }
  });
}

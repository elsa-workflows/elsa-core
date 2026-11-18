import { createRequire } from "node:module";
import { describe, expect, it } from "vitest";

const require = createRequire(import.meta.url);
const vite = createRequire(require.resolve("vite/package.json"));
const vitest = createRequire(require.resolve("vitest/package.json"));
const tinyglobby = createRequire(vite.resolve("tinyglobby"));
const fdir = createRequire(tinyglobby.resolve("fdir"));

type GlobMatcher = {
  (glob: string): (path: string) => boolean;
  makeRe: (glob: string) => RegExp;
  parse: (glob: string) => unknown;
};

describe.each([
  { name: "Vite", requester: vite },
  { name: "Vitest", requester: vitest },
  { name: "Tinyglobby", requester: tinyglobby },
  { name: "Fdir", requester: fdir },
])("$name build globs", ({ requester }) => {
  const match = requester("picomatch") as GlobMatcher;

  it("uses the hardened build matcher with normal file patterns", () => {
    expect(requester("picomatch/package.json")).toMatchObject({ name: "@elsa-workflows/picomatch-build", private: true });
    const sourceFile = match("src/**/*.{ts,tsx}");
    expect(sourceFile("src/areas/entry.tsx")).toBe(true);
    expect(sourceFile("src/areas/entry.css")).toBe(false);
    expect(match("src/@(public|private).ts")("src/public.ts")).toBe(true);
  });

  it.each(["+(feature)", "*(feature)"])("rejects repeated extglob %s", (pattern) => {
    expect(() => match(pattern)).toThrow("Repeated extglobs are disabled");
    expect(() => match.makeRe(pattern)).toThrow("Repeated extglobs are disabled");
    expect(() => match.parse(pattern)).toThrow("Repeated extglobs are disabled");
  });
});

#!/usr/bin/env python3
"""Build isolated native X6 and DomInterop assets from reviewed Elsa 3 sources."""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile


REPLACEMENTS: dict[str, list[tuple[str, str, str]]] = {
    "designer.ts": [
        ("designer-bpmn-css-import", 'import "../css/designer.bpmn.css";\n', ""),
    ],
    "designer/internal/init.ts": [
        ("bpmn-shapes-import", "import {registerBpmnShapes} from '../bpmn/shapes';\n", ""),
        ("bpmn-shapes-registration", "    registerBpmnShapes();\n", ""),
    ],
    "designer/api/index.ts": [
        ("bpmn-api-export", "export * from './bpmn-designer';\n", ""),
    ],
    "designer/api/find-graph.ts": [
        ("bpmn-registry-import", "import {bpmnGraphBindings} from '../bpmn/graph-registry';\n", ""),
        (
            "bpmn-registry-fallback",
            "    return graphBindings[graphId]?.graph ?? bpmnGraphBindings[graphId]?.graph ?? null;\n",
            "    return graphBindings[graphId]?.graph ?? null;\n",
        ),
    ],
    "designer/api/dispose-graph.ts": [
        ("bpmn-registry-import", 'import {disposeBpmnGraphBinding} from "../bpmn/graph-registry";\n', ""),
        (
            "bpmn-registry-fallback",
            "    if (!binding) {\n        disposeBpmnGraphBinding(graphId);\n        return;\n    }\n",
            "    if (!binding)\n        return;\n",
        ),
    ],
}


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    return sha256_bytes(path.read_bytes())


def run_logged(command: list[str], cwd: Path, log_path: Path, env: dict[str, str], timeout: int = 300) -> str:
    try:
        result = subprocess.run(
            command, cwd=cwd, env=env, text=True, stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT, timeout=timeout,
        )
        log = result.stdout
        return_code = result.returncode
    except subprocess.TimeoutExpired as error:
        log = error.stdout or ""
        if isinstance(log, bytes):
            log = log.decode("utf-8", errors="replace")
        log += f"\nTimed out after {timeout} seconds.\n"
        return_code = -1
    log_path.write_text(log, encoding="utf-8")
    if return_code:
        print(log, end="")
        raise RuntimeError(f"Command failed ({return_code}); full output: {log_path}")
    return log


def copy_file(source: Path, destination: Path, source_inputs: dict[str, str], source_paths: dict[str, str], key: str) -> None:
    data = source.read_bytes()
    source_inputs[key] = sha256_bytes(data)
    source_paths[key] = str(source.resolve(strict=True))
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(data)


def stage_manifest(source_manifest: Path, destination: Path, source_inputs: dict[str, str], source_paths: dict[str, str], key: str) -> None:
    raw = source_manifest.read_bytes()
    source_inputs[key] = sha256_bytes(raw)
    source_paths[key] = str(source_manifest.resolve(strict=True))
    manifest = json.loads(raw)
    # Deliberately omit scripts: Designer's normal scripts run BPMN generation/tests.
    sanitized = {name: manifest[name] for name in ("name", "version", "dependencies", "devDependencies") if name in manifest}
    destination.write_text(json.dumps(sanitized, indent=2) + "\n", encoding="utf-8")


def apply_replacements(relative_path: str, raw: bytes, transformations: list[dict]) -> bytes:
    text = raw.decode("utf-8")
    for name, before, after in REPLACEMENTS.get(relative_path, []):
        count = text.count(before)
        if count != 1:
            raise RuntimeError(f"Fail-closed replacement {name} in {relative_path}: expected 1 exact match, found {count}")
        text = text.replace(before, after, 1)
        transformations.append({"file": relative_path, "name": name, "exact_matches": count})
    return text.encode("utf-8")


def copy_native_designer_sources(source_clientlib: Path, stage_clientlib: Path, source_inputs: dict[str, str], source_paths: dict[str, str], staged_hashes: dict[str, str], transformations: list[dict]) -> None:
    source_root = source_clientlib / "src"
    destination_root = stage_clientlib / "src"
    designer_entry = source_root / "designer.ts"
    if not designer_entry.is_file():
        raise RuntimeError(f"Missing native X6 entry: {designer_entry}")

    allowed_root = source_root / "designer"
    for source in sorted([designer_entry, *allowed_root.rglob("*")]):
        if not source.is_file():
            continue
        relative = source.relative_to(source_root)
        parts = [part.casefold() for part in relative.parts]
        if any("bpmn" in part for part in parts):
            continue
        if "__tests__" in parts or source.name.casefold().endswith((".test.ts", ".spec.ts", ".test.tsx", ".spec.tsx")):
            continue
        if source.suffix not in {".ts", ".tsx", ".js", ".jsx"}:
            continue
        raw = source.read_bytes()
        source_key = f"designer/{relative.as_posix()}"
        source_inputs[source_key] = sha256_bytes(raw)
        source_paths[source_key] = str(source.resolve(strict=True))
        staged = apply_replacements(relative.as_posix(), raw, transformations)
        destination = destination_root / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(staged)
        staged_hashes[f"designer/{relative.as_posix()}"] = sha256_bytes(staged)

    expected_replacements = {
        (relative_path, name)
        for relative_path, replacements in REPLACEMENTS.items()
        for name, _before, _after in replacements
    }
    actual_replacements = {(item["file"], item["name"]) for item in transformations}
    if actual_replacements != expected_replacements:
        raise RuntimeError(f"Replacement set mismatch: expected {sorted(expected_replacements)}, got {sorted(actual_replacements)}")

    # Only the two native designer styles are inputs. The BPMN stylesheet is never read or copied.
    for name in ("designer.v1.css", "designer.v2.css"):
        source = source_clientlib / "css" / name
        destination = stage_clientlib / "css" / name
        copy_file(source, destination, source_inputs, source_paths, f"designer/css/{name}")
        staged_hashes[f"designer/css/{name}"] = sha256_file(destination)


def create_designer_webpack_config(stage_clientlib: Path) -> None:
    config = r'''const path = require('path');
module.exports = {
  context: __dirname,
  entry: './src/designer.ts',
  output: {
    filename: 'designer.entry.js',
    path: path.resolve(__dirname, 'wwwroot'),
    library: { type: 'module' },
    module: true,
    environment: { module: true },
  },
  experiments: { outputModule: true },
  devtool: 'source-map',
  mode: 'development',
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        include: [path.resolve(__dirname, 'src')],
        exclude: /node_modules/,
        use: [{ loader: 'ts-loader', options: { transpileOnly: true } }],
      },
      { test: /\.css$/, use: ['style-loader', 'css-loader'] },
      { test: /\.(eot|woff(2)?|ttf|otf|svg)$/i, type: 'asset' },
    ],
  },
  resolve: { extensions: ['.tsx', '.ts', '.js'] },
};
'''
    (stage_clientlib / "webpack.native.config.cjs").write_text(config, encoding="utf-8")


def create_graph_runner(stage_clientlib: Path) -> None:
    runner = r'''const fs = require('fs');
const path = require('path');
const webpack = require('webpack');
const configPath = path.resolve(process.argv[2]);
const reportPath = path.resolve(process.argv[3]);
const config = require(configPath);
webpack(config, (error, stats) => {
  if (error) {
    console.error(error.stack || error);
    process.exitCode = 1;
    return;
  }
  const result = stats.toJson({ all: false, errors: true, warnings: true, chunks: true });
  if (stats.hasErrors()) {
    console.error(stats.toString({ all: false, errors: true, errorDetails: true, warnings: true }));
    process.exitCode = 1;
    return;
  }
  const modules = new Set();
  const addModule = module => {
    if (!module) return;
    for (const value of [module.resource, module.nameForCondition && module.nameForCondition()]) {
      if (typeof value === 'string' && value) modules.add(value);
    }
    if (typeof module.identifier === 'function') {
      const value = module.identifier();
      if (typeof value === 'string' && value) modules.add(value);
    }
    if (module.modules && typeof module.modules[Symbol.iterator] === 'function') {
      for (const child of module.modules) addModule(child);
    }
  };
  for (const module of stats.compilation.modules) addModule(module);
  const modulePaths = [...modules].sort();
  const forbidden = modulePaths.filter(name => /bpmn/i.test(name));
  if (forbidden.length) {
    console.error('Forbidden BPMN module path(s) in webpack graph:');
    forbidden.forEach(name => console.error(name));
    process.exitCode = 2;
    return;
  }
  fs.writeFileSync(reportPath, JSON.stringify({ modules: modulePaths, warnings: result.warnings || [], chunks: result.chunks || [] }, null, 2));
});
'''
    (stage_clientlib / "run-webpack.cjs").write_text(runner, encoding="utf-8")


def npm_command(stage: Path, lock_source: Path, package_source: Path, label: str, npm: str, cache: Path, user_npmrc: Path, global_npmrc: Path, logs: Path, source_inputs: dict[str, str], source_paths: dict[str, str], env: dict[str, str]) -> None:
    stage.mkdir(parents=True, exist_ok=True)
    stage_manifest(package_source, stage / "package.json", source_inputs, source_paths, f"{label}/package.json")
    copy_file(lock_source, stage / "package-lock.json", source_inputs, source_paths, f"{label}/reviewed-package-lock.json")
    cache.mkdir(parents=True, exist_ok=True)
    os.chmod(cache, 0o700)
    command = [npm, "ci", "--ignore-scripts", "--no-audit", "--no-fund", "--cache", str(cache), "--userconfig", str(user_npmrc), "--globalconfig", str(global_npmrc)]
    run_logged(command, stage, logs / f"npm-ci-{label}.log", env)


def node_tool_versions(node: str, designer_stage: Path, env: dict[str, str]) -> dict[str, str]:
    code = r'''const names = ['webpack', 'webpack-cli', 'typescript', 'ts-loader', 'style-loader', 'css-loader'];
const output = {};
for (const name of names) {
  try { output[name] = require(require.resolve(name + '/package.json', { paths: [process.argv[1]] })).version; }
  catch { output[name] = null; }
}
process.stdout.write(JSON.stringify(output));'''
    result = subprocess.run([node, "-e", code, str(designer_stage)], env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True, timeout=30)
    return json.loads(result.stdout)


def git_output(root: Path, *args: str) -> str:
    env = {"PATH": os.environ.get("PATH", ""), "GIT_OPTIONAL_LOCKS": "0"}
    result = subprocess.run(
        ["git", "--no-optional-locks", "-C", str(root), *args],
        env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        check=True, timeout=30,
    )
    return result.stdout.strip()


def assert_clean_source(root: Path) -> None:
    status = git_output(root, "status", "--porcelain=v1", "--untracked-files=all")
    if status:
        raise RuntimeError(f"Source checkout must be clean at start and finish; status was:\n{status}")


def rehash_source_inputs(source_paths: dict[str, str], expected_hashes: dict[str, str]) -> None:
    if source_paths.keys() != expected_hashes.keys():
        raise RuntimeError("Recorded source path/hash key sets differ")
    changed = []
    for key, path_text in sorted(source_paths.items()):
        path = Path(path_text)
        if not path.is_file() or sha256_file(path) != expected_hashes[key]:
            changed.append(key)
    if changed:
        raise RuntimeError(f"Source input changed during preparation: {changed}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--cache", required=True, type=Path)
    parser.add_argument("--expected-head", required=True)
    args = parser.parse_args()
    if not re.fullmatch(r"[0-9a-f]{40}", args.expected_head):
        raise RuntimeError("--expected-head must be a full lowercase Git commit SHA")

    os.umask(0o077)
    root = args.root.resolve(strict=True)
    output = args.output.resolve()
    cache_root = args.cache.resolve()
    if output == root or root in output.parents or cache_root == root or root in cache_root.parents:
        raise RuntimeError("Output and cache paths must be outside the repository")
    output.mkdir(parents=True, exist_ok=True)
    if any(output.iterdir()):
        raise RuntimeError(f"Output directory must be empty: {output}")
    os.chmod(output, 0o700)
    cache_root.mkdir(parents=True, exist_ok=True)
    os.chmod(cache_root, 0o700)
    logs = output / "logs"
    logs.mkdir(mode=0o700)
    os.chmod(logs, 0o700)

    source_commit = git_output(root, "rev-parse", "HEAD")
    if source_commit != args.expected_head:
        raise RuntimeError(f"Expected source HEAD {args.expected_head}; found {source_commit}")
    assert_clean_source(root)

    node = shutil.which("node")
    npm = shutil.which("npm")
    if not node or not npm:
        raise RuntimeError("Node 22 and npm must be on PATH")
    work = Path(tempfile.mkdtemp(prefix=".native-assets-build-", dir=output))
    os.chmod(work, 0o700)
    home = work / "home"
    tmp = work / "tmp"
    home.mkdir(mode=0o700)
    tmp.mkdir(mode=0o700)
    env = {"PATH": os.environ.get("PATH", ""), "HOME": str(home), "TMPDIR": str(tmp)}
    for name in ("LANG", "LC_ALL"):
        if name in os.environ:
            env[name] = os.environ[name]
    node_version = subprocess.check_output([node, "--version"], env=env, text=True, timeout=30).strip()
    npm_version = subprocess.check_output([npm, "--version"], env=env, text=True, timeout=30).strip()
    if not node_version.startswith("v22."):
        raise RuntimeError(f"Node 22 is required; found {node_version}")

    studio = root / "studio/src"
    designer = studio / "modules/Elsa.Studio.Workflows.Designer"
    dom = studio / "framework/Elsa.Studio.DomInterop"
    lock_dir = root / "scripts/integration-program/consolidated-build/studio-clientlib-lockfiles"
    designer_lock = lock_dir / "designer.package-lock.json"
    dom_lock = lock_dir / "dom-interop.package-lock.json"
    designer_manifest = designer / "ClientLib/package.json"
    dom_manifest = dom / "ClientLib/package.json"
    designer_tsconfig = designer / "ClientLib/tsconfig.json"
    dom_webpack = dom / "ClientLib/webpack.config.js"
    dom_tsconfig = dom / "ClientLib/tsconfig.json"

    for path in (designer_lock, dom_lock, designer_manifest, dom_manifest, designer_tsconfig, dom_webpack, dom_tsconfig):
        if not path.is_file():
            raise RuntimeError(f"Missing reviewed source input: {path}")

    # Separate isolated config files prevent ambient user/global credentials and npm's duplicate-file error.
    user_npmrc = work / "private-user.npmrc"
    global_npmrc = work / "private-global.npmrc"
    for npmrc in (user_npmrc, global_npmrc):
        npmrc.write_text("fund=false\naudit=false\nupdate-notifier=false\n", encoding="utf-8")
        os.chmod(npmrc, 0o600)

    source_inputs: dict[str, str] = {}
    source_paths: dict[str, str] = {}
    staged_hashes: dict[str, str] = {}
    transformations: list[dict] = []
    output_hashes: dict[str, str] = {}
    try:
        designer_stage = work / "designer/ClientLib"
        dom_stage = work / "dom-interoperability/ClientLib"
        npm_command(designer_stage, designer_lock, designer_manifest, "designer", npm, cache_root / "designer-npm-cache", user_npmrc, global_npmrc, logs, source_inputs, source_paths, env)
        npm_command(dom_stage, dom_lock, dom_manifest, "dominterop", npm, cache_root / "dominterop-npm-cache", user_npmrc, global_npmrc, logs, source_inputs, source_paths, env)

        copy_native_designer_sources(designer / "ClientLib", designer_stage, source_inputs, source_paths, staged_hashes, transformations)
        copy_file(designer_tsconfig, designer_stage / "tsconfig.json", source_inputs, source_paths, "designer/tsconfig.json")
        staged_hashes["designer/tsconfig.json"] = sha256_file(designer_stage / "tsconfig.json")
        create_designer_webpack_config(designer_stage)
        create_graph_runner(designer_stage)
        staged_hashes["designer/webpack.native.config.cjs"] = sha256_file(designer_stage / "webpack.native.config.cjs")
        staged_hashes["designer/run-webpack.cjs"] = sha256_file(designer_stage / "run-webpack.cjs")

        dom_src = dom / "ClientLib/src"
        for source in sorted(dom_src.rglob("*")):
            if not source.is_file():
                continue
            relative = source.relative_to(dom_src)
            destination = dom_stage / "src" / relative
            copy_file(source, destination, source_inputs, source_paths, f"dominterop/src/{relative.as_posix()}")
            staged_hashes[f"dominterop/src/{relative.as_posix()}"] = sha256_file(destination)
        copy_file(dom_webpack, dom_stage / "webpack.config.js", source_inputs, source_paths, "dominterop/webpack.config.js")
        staged_hashes["dominterop/webpack.config.js"] = sha256_file(dom_stage / "webpack.config.js")
        copy_file(dom_tsconfig, dom_stage / "tsconfig.json", source_inputs, source_paths, "dominterop/tsconfig.json")
        staged_hashes["dominterop/tsconfig.json"] = sha256_file(dom_stage / "tsconfig.json")
        create_graph_runner(dom_stage)
        staged_hashes["dominterop/run-webpack.cjs"] = sha256_file(dom_stage / "run-webpack.cjs")

        tool_versions = node_tool_versions(node, designer_stage, env)
        if not all(tool_versions.get(name) for name in ("webpack", "webpack-cli", "typescript", "ts-loader", "style-loader", "css-loader")):
            raise RuntimeError(f"Missing required locked Designer tools: {tool_versions}")

        for stage, config_name, graph_name, label in (
            (designer_stage, "webpack.native.config.cjs", "designer-module-graph.json", "designer"),
            (dom_stage, "webpack.config.js", "dominterop-module-graph.json", "dominterop"),
        ):
            run_logged(
                [node, str(stage / "run-webpack.cjs"), str(stage / config_name), str(output / "logs" / graph_name)],
                stage,
                logs / f"webpack-{label}.log",
                env,
            )

        assets_root = output / "assets"
        designer_out = designer_stage / "wwwroot"
        dom_out = dom_stage.parent / "wwwroot"
        designer_assets = assets_root / "Elsa.Studio.Workflows.Designer"
        dom_assets = assets_root / "Elsa.Studio.DomInterop"
        designer_assets.mkdir(parents=True, exist_ok=True)
        dom_assets.mkdir(parents=True, exist_ok=True)
        for name in ("designer.entry.js", "designer.entry.js.map"):
            source = designer_out / name
            if not source.is_file() or source.stat().st_size == 0:
                raise RuntimeError(f"Missing native Designer output: {source}")
            shutil.copy2(source, designer_assets / name)
        css_root = designer / "ClientLib/css"
        for name in ("designer.v1.css", "designer.v2.css"):
            source = css_root / name
            copy_file(source, designer_assets / name, source_inputs, source_paths, f"designer/css-output-source/{name}")
        shutil.copy2(designer_assets / "designer.v2.css", designer_assets / "designer.css")
        for name in ("dom.entry.js", "clipboard.entry.js", "files.entry.js", "dom.entry.js.map", "clipboard.entry.js.map", "files.entry.js.map"):
            source = dom_out / name
            if not source.is_file() or source.stat().st_size == 0:
                raise RuntimeError(f"Missing DomInterop output: {source}")
            shutil.copy2(source, dom_assets / name)

        for path in sorted(assets_root.rglob("*")):
            if path.is_file():
                output_hashes[path.relative_to(output).as_posix()] = sha256_file(path)

        if git_output(root, "rev-parse", "HEAD") != source_commit:
            raise RuntimeError("Source HEAD changed during preparation")
        rehash_source_inputs(source_paths, source_inputs)
        assert_clean_source(root)

        receipt = {
            "created_utc": datetime.now(timezone.utc).isoformat(),
            "source_root": str(root),
            "source_head": source_commit,
            "builder_sha256": sha256_file(Path(__file__)),
            "node_version": node_version,
            "npm_version": npm_version,
            "tool_versions": tool_versions,
            "reviewed_locks": {
                "designer": {"path": str(designer_lock), "sha256": source_inputs["designer/reviewed-package-lock.json"]},
                "dominterop": {"path": str(dom_lock), "sha256": source_inputs["dominterop/reviewed-package-lock.json"]},
            },
            "transformations": transformations,
            "source_input_sha256": source_inputs,
            "source_input_paths": source_paths,
            "staged_source_sha256": staged_hashes,
            "module_graphs": {
                "designer": json.loads((output / "logs/designer-module-graph.json").read_text(encoding="utf-8")),
                "dominterop": json.loads((output / "logs/dominterop-module-graph.json").read_text(encoding="utf-8")),
            },
            "output_sha256": output_hashes,
            "scope": "isolated native X6 Designer and unchanged DomInterop bundles only; no React Flow entry, .NET restore, tests, generated-type step, hosts, or full Studio acceptance claimed",
        }
        (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        os.chmod(output / "receipt.json", 0o600)
        for path in assets_root.rglob("*"):
            if path.is_dir():
                os.chmod(path, 0o700)
            elif path.is_file():
                os.chmod(path, 0o600)
    finally:
        shutil.rmtree(work, ignore_errors=True)

    try:
        if git_output(root, "rev-parse", "HEAD") != source_commit:
            raise RuntimeError("Source HEAD changed before success")
        rehash_source_inputs(source_paths, source_inputs)
        assert_clean_source(root)
    except RuntimeError:
        (output / "receipt.json").unlink(missing_ok=True)
        raise

    os.chmod(output, 0o700)
    print(f"Prepared native assets under {output / 'assets'}")
    print(f"Receipt: {output / 'receipt.json'}")
    print(f"Node: {node_version}; npm: {npm_version}; source HEAD: {source_commit}")


if __name__ == "__main__":
    main()

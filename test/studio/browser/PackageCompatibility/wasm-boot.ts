import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';

type PlatformRole = { role: string; served: string; content_type: string; group?: string; virtual?: string };
type BootPolicy = {
  format: string; framework: string; main_assembly: string; maximum_boot_bytes: number; maximum_managed_resources: number;
  checks: string[]; configuration_fields: string[]; resource_groups: string[]; mandatory_managed: string[]; platform: PlatformRole[];
  optional_configuration_fields?: string[]; optional_resource_groups?: string[];
};
export const wasmBootPolicy = JSON.parse(readFileSync(new URL('./wasm-boot-policy.json', import.meta.url), 'utf8')) as
  BootPolicy & { json_formats: Record<string, Partial<BootPolicy>> };
type Resource = { path: string; sha256: string; bytes: number; content_type: string; owner: 'platform' | 'package' | 'fixture'; boot_configuration_sha256?: string };
export type ObservedBootResource = Resource & { status: number; requested: boolean };
type PlatformBinding = { role: string; path: string; sha256: string };
type ManagedBinding = { path: string; sha256: string; owner: 'package' | 'fixture' };
export type WasmBootProof = {
  format: string; checks: Record<string, boolean>; platform_bindings: PlatformBinding[]; managed_bindings: ManagedBinding[];
  bootstrap_sha256?: string; configuration_sha256?: string;
};
const hash = (body: Buffer) => createHash('sha256').update(body).digest('hex');
const managedPath = /^\/_framework\/(Elsa\.[A-Za-z0-9_.-]+)\.([a-z0-9]{10})\.wasm$/;
const sameKeys = (value: any, fields: string[]) => value !== null && typeof value === 'object' && !Array.isArray(value) &&
  Object.keys(value).sort().join('|') === [...fields].sort().join('|');
const require = (condition: unknown, reason: string): void => { if (!condition) throw new Error(reason); };
export function bootPolicy(framework: string): BootPolicy {
  require(framework === wasmBootPolicy.framework || Object.hasOwn(wasmBootPolicy.json_formats, framework), 'unknown_boot_framework');
  return framework === wasmBootPolicy.framework ? wasmBootPolicy : { ...wasmBootPolicy, ...wasmBootPolicy.json_formats[framework] };
}
const managedPattern = (framework: string) => framework === 'net8.0' ? /^\/_framework\/(Elsa\.[A-Za-z0-9_.-]+)\.wasm$/ : managedPath;
const schemaKeys = (value: any, required: string[], optional: string[] = []): boolean =>
  value !== null && typeof value === 'object' && !Array.isArray(value) && required.every(key => Object.hasOwn(value, key)) &&
  Object.keys(value).every(key => required.includes(key) || optional.includes(key));
const sri = (digest: string): string => {
  require(/^[a-f0-9]{64}$/.test(digest), 'invalid_boot_binding_hash');
  return 'sha256-' + Buffer.from(digest, 'hex').toString('base64');
};

export function platformRole(path: string, framework = 'net10.0'): string | undefined {
  return bootPolicy(framework).platform.find(role => {
    const escaped = ('/_framework/' + role.served).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    return new RegExp('^' + escaped.replace('\\{fingerprint\\}', '[a-z0-9]{10}') + '$').test(path);
  })?.role;
}

// Parse only observed, hash-matched JSON (embedded for net10). Never evaluate the loader.
export function parseWasmBootstrap(body: Buffer, platform: PlatformBinding[], managed: ManagedBinding[], framework = 'net10.0'): string {
  const policy = bootPolicy(framework);
  require(body.length > 0 && body.length <= policy.maximum_boot_bytes, 'boot_script_limit');
  const script = new TextDecoder('utf-8', { fatal: true }).decode(body);
  let raw = script;
  if (framework === 'net10.0') {
    const start = '/*json-start*/', end = '/*json-end*/';
    const left = script.indexOf(start), right = script.indexOf(end);
    require(left >= 0 && right > left && script.lastIndexOf(start) === left && script.lastIndexOf(end) === right &&
      /\.withConfig\(\s*$/.test(script.slice(0, left)) && script.slice(right + end.length).startsWith(');'), 'unknown_boot_format');
    raw = script.slice(left + start.length, right);
  }
  const config = JSON.parse(raw);
  require(schemaKeys(config, policy.configuration_fields, policy.optional_configuration_fields) && config.mainAssemblyName === policy.main_assembly,
    'unknown_boot_configuration');
  if (framework !== 'net10.0') require([-1, 0].includes(config.debugLevel) &&
    ['sharded', 'all', 'invariant', 'custom', 'hybrid'].includes(config.globalizationMode) &&
    ['cacheBootResources', 'linkerEnabled'].every(field => !Object.hasOwn(config, field) || typeof config[field] === 'boolean'), 'unknown_release_boot_configuration');
  const listed = config.resources;
  require(schemaKeys(listed, policy.resource_groups, policy.optional_resource_groups), 'unknown_boot_resource_schema');
  const fingerprints = listed.fingerprinting ?? {};
  if (framework !== 'net10.0') require(fingerprints !== null && typeof fingerprints === 'object' && !Array.isArray(fingerprints) &&
    Object.keys(fingerprints).length <= 4096 && Object.entries(fingerprints).every(([name, virtual]) =>
      /^[A-Za-z0-9_.-]+$/.test(name) && typeof virtual === 'string' && /^[A-Za-z0-9_.-]+$/.test(virtual)), 'unknown_boot_fingerprint_map');
  const roles = new Map(platform.map(binding => [platformRole(binding.path, framework), binding]));
  require(platform.length === policy.platform.length && roles.size === platform.length && !roles.has(undefined), 'missing_boot_platform_bindings');
  for (const role of policy.platform) {
    if (!role.group) continue;
    const rows = listed[role.group], binding = roles.get(role.role)!;
    if (framework !== 'net10.0') {
      const name = binding.path.slice('/_framework/'.length);
      require(sameKeys(rows, [name]) && rows[name] === sri(binding.sha256), 'native_boot_integrity_mismatch');
      if (framework === 'net9.0') require(fingerprints[name] === role.virtual, 'native_boot_fingerprint_mismatch');
      continue;
    }
    require(Array.isArray(rows) && rows.length === 1 && sameKeys(rows[0], role.group === 'wasmNative' ? ['name', 'hash', 'cache'] : ['name']),
      'unknown_native_boot_group');
    require(rows[0].name === binding.path.slice('/_framework/'.length), 'native_boot_name_mismatch');
    if (role.group === 'wasmNative')
      require(rows[0].hash === sri(binding.sha256) && rows[0].cache === 'force-cache', 'native_boot_integrity_mismatch');
  }
  const indexed = new Map<string, any>(), virtualPaths = new Set<string>();
  for (const group of ['assembly', 'coreAssembly']) {
    let rows = listed[group];
    if (framework !== 'net10.0') {
      require(rows !== null && typeof rows === 'object' && !Array.isArray(rows), 'unknown_managed_boot_group');
      require(Object.keys(rows).length <= 1024 && (Object.keys(rows).length > 0 || group === 'coreAssembly'), 'unknown_managed_boot_group');
      rows = Object.entries(rows).map(([name, digest]) => ({ name, virtualPath: fingerprints[name] ?? name, hash: digest, cache: 'force-cache' }));
    } else require(Array.isArray(rows) && rows.length > 0 && rows.length <= 1024, 'unknown_managed_boot_group');
    for (const row of rows) {
      require(sameKeys(row, ['virtualPath', 'name', 'hash', 'cache']) && typeof row.name === 'string' &&
        /^[A-Za-z0-9_.-]+\.wasm$/.test(row.name) && typeof row.virtualPath === 'string' &&
        /^[A-Za-z0-9_.-]+\.wasm$/.test(row.virtualPath) && !indexed.has(row.name.toLowerCase()) &&
        !virtualPaths.has(row.virtualPath.toLowerCase()) && row.cache === 'force-cache',
      'invalid_managed_boot_resource');
      indexed.set(row.name.toLowerCase(), row);
      virtualPaths.add(row.virtualPath.toLowerCase());
    }
  }
  require(managed.length > 0 && managed.length <= policy.maximum_managed_resources, 'missing_managed_boot_bindings');
  const expected = new Set<string>(), assemblies = new Set<string>();
  for (const binding of managed) {
    const match = binding.path.match(managedPattern(framework));
    require(match && ['package', 'fixture'].includes(binding.owner), 'invalid_managed_boot_binding');
    const assembly = match![1], name = binding.path.slice('/_framework/'.length), row = indexed.get(name.toLowerCase());
    require((binding.owner === 'fixture') === (assembly === policy.main_assembly) && !expected.has(name.toLowerCase()) &&
      row?.name === name && row.virtualPath === assembly + '.wasm' && row.hash === sri(binding.sha256), 'managed_boot_binding_mismatch');
    expected.add(name.toLowerCase()); assemblies.add(assembly);
  }
  require([...policy.mandatory_managed, policy.main_assembly].every(name => assemblies.has(name)), 'missing_mandatory_boot_subset');
  const elsaNames = [...indexed.keys()].filter(name => name.startsWith('elsa.'));
  require(elsaNames.length === expected.size && elsaNames.every(name => expected.has(name)), 'unbound_elsa_boot_resource');
  return hash(Buffer.from(raw));
}

// The existing page response listener supplies bytes. This observer performs no requests.
export class WasmBootObserver {
  private readonly policy: BootPolicy;
  private readonly platform: Resource[];
  private readonly managed: Resource[];
  private bootstrap: Buffer | undefined;

  constructor(resources: Resource[], private readonly framework: string) {
    this.policy = bootPolicy(framework);
    this.platform = resources.filter(resource => resource.owner === 'platform');
    this.managed = resources.filter(resource => resource.owner !== 'platform' && resource.path.startsWith('/_framework/'));
    require(this.platform.length === this.policy.platform.length && this.platform.every(row => platformRole(row.path, framework)), 'unknown_boot_platform_inventory');
    require(new Set(this.platform.map(row => platformRole(row.path, framework))).size === this.platform.length, 'duplicate_boot_platform_inventory');
    require(this.managed.length > 0 && this.managed.length <= this.policy.maximum_managed_resources &&
      this.managed.every(row => managedPattern(framework).test(row.path) && ['package', 'fixture'].includes(row.owner)), 'unknown_boot_managed_inventory');
    const manifest = this.platform.find(row => platformRole(row.path, framework) === 'manifest')!;
    require(typeof manifest.boot_configuration_sha256 === 'string' && /^[a-f0-9]{64}$/.test(manifest.boot_configuration_sha256), 'missing_original_boot_configuration_binding');
  }

  observe(resource: ObservedBootResource, body: Buffer): void {
    const expected = this.platform.find(row => platformRole(row.path, this.framework) === 'manifest');
    if (resource.path === expected?.path && resource.status === 200 && resource.owner === 'platform' &&
        resource.content_type === expected.content_type && resource.sha256 === expected.sha256 && resource.bytes === expected.bytes &&
        body.length === expected.bytes && hash(body) === expected.sha256)
      this.bootstrap = body;
  }

  proof(observed: ObservedBootResource[], managedCallback: boolean): WasmBootProof {
    const checks = Object.fromEntries(this.policy.checks.map(name => [name, false]));
    const proof: WasmBootProof = { format: this.policy.format, checks,
      platform_bindings: this.platform.map(row => ({ role: platformRole(row.path, this.framework)!, path: row.path, sha256: row.sha256 })),
      managed_bindings: this.managed.map(row => ({ path: row.path, sha256: row.sha256, owner: row.owner as ManagedBinding['owner'] })) };
    const matched = (expected: Resource) => observed.some(row => row.path === expected.path && row.sha256 === expected.sha256 &&
      row.bytes === expected.bytes && row.content_type === expected.content_type && row.owner === expected.owner && row.status === 200 && row.requested === true);
    checks.platform_resources = this.platform.every(matched);
    if (this.bootstrap) {
      proof.bootstrap_sha256 = hash(this.bootstrap);
      try {
        proof.configuration_sha256 = parseWasmBootstrap(this.bootstrap, proof.platform_bindings, proof.managed_bindings, this.framework);
        checks.configuration = proof.configuration_sha256 === this.platform.find(row => platformRole(row.path, this.framework) === 'manifest')!.boot_configuration_sha256;
      } catch { /* Unknown, malformed or unbound boot configurations remain incomplete evidence. */ }
    }
    checks.managed_resources = checks.configuration && this.managed.every(matched);
    checks.managed_callback = managedCallback;
    return proof;
  }
}

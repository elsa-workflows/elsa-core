import "server-only";
import { promises as fs } from "node:fs";
import path from "node:path";
import { studioConfigSchema, type StudioConfig, type StudioConfigInput } from "./schema";

/**
 * Resolves the runtime configuration on the server.
 *
 * Precedence (highest first):
 *   1. `STUDIO_CONFIG` env var holding a JSON document.
 *   2. The file pointed to by `STUDIO_CONFIG_PATH` (default `./studio.config.json`).
 *   3. Individual `STUDIO_*` env vars, which overlay the document above:
 *        STUDIO_TITLE, STUDIO_SERVER_URL, STUDIO_SERVER_NAME, STUDIO_TENANT_ID,
 *        STUDIO_AUTH_PROVIDER, STUDIO_API_KEY,
 *        STUDIO_OIDC_AUTHORITY, STUDIO_OIDC_CLIENT_ID, STUDIO_OIDC_SCOPE,
 *        STUDIO_FEATURES (comma separated module ids to enable, e.g. "labels,secrets").
 *
 * Being evaluated per request means the file can be replaced in a running container
 * without a rebuild.
 */
export async function loadStudioConfig(): Promise<StudioConfig> {
  const raw = (await readDocument()) ?? {};
  applyEnvOverlay(raw);
  const parsed = studioConfigSchema.safeParse(raw);
  if (!parsed.success) {
    const issues = parsed.error.issues.map((i) => `${i.path.join(".") || "<root>"}: ${i.message}`).join("; ");
    throw new Error(`Invalid studio configuration: ${issues}`);
  }
  return parsed.data;
}

async function readDocument(): Promise<Record<string, unknown> | undefined> {
  const inline = process.env.STUDIO_CONFIG;
  if (inline) return JSON.parse(inline) as Record<string, unknown>;

  const file = path.resolve(/* turbopackIgnore: true */ process.cwd(), process.env.STUDIO_CONFIG_PATH ?? "studio.config.json");
  try {
    const text = await fs.readFile(file, "utf8");
    return JSON.parse(text) as Record<string, unknown>;
  } catch (err) {
    if ((err as NodeJS.ErrnoException).code === "ENOENT") return undefined;
    throw err;
  }
}

function applyEnvOverlay(doc: Record<string, unknown>) {
  const env = process.env;
  const cfg = doc as Partial<StudioConfigInput> & Record<string, unknown>;

  if (env.STUDIO_TITLE) cfg.title = env.STUDIO_TITLE;

  if (env.STUDIO_SERVER_URL) {
    const servers = Array.isArray(cfg.servers) ? [...cfg.servers] : [];
    const first = servers[0] ?? { url: "" };
    servers[0] = {
      ...first,
      url: env.STUDIO_SERVER_URL,
      name: env.STUDIO_SERVER_NAME ?? first.name ?? "Default",
      tenantId: env.STUDIO_TENANT_ID ?? first.tenantId,
    };
    cfg.servers = servers;
  }

  const auth = { ...(cfg.auth ?? {}) } as NonNullable<StudioConfigInput["auth"]> & Record<string, unknown>;
  if (env.STUDIO_AUTH_PROVIDER) auth.provider = env.STUDIO_AUTH_PROVIDER as never;
  if (env.STUDIO_API_KEY) auth.apiKey = { ...(auth.apiKey ?? {}), key: env.STUDIO_API_KEY };
  if (env.STUDIO_OIDC_AUTHORITY || env.STUDIO_OIDC_CLIENT_ID || env.STUDIO_OIDC_SCOPE) {
    auth.oidc = {
      ...(auth.oidc ?? {}),
      ...(env.STUDIO_OIDC_AUTHORITY ? { authority: env.STUDIO_OIDC_AUTHORITY } : {}),
      ...(env.STUDIO_OIDC_CLIENT_ID ? { clientId: env.STUDIO_OIDC_CLIENT_ID } : {}),
      ...(env.STUDIO_OIDC_SCOPE ? { scope: env.STUDIO_OIDC_SCOPE } : {}),
    };
  }
  cfg.auth = auth;

  if (env.STUDIO_FEATURES) {
    const features = { ...(cfg.features ?? {}) } as Record<string, { enabled?: boolean }>;
    for (const id of env.STUDIO_FEATURES.split(",").map((s) => s.trim()).filter(Boolean)) {
      features[id] = { ...(features[id] ?? {}), enabled: true };
    }
    cfg.features = features as never;
  }
}

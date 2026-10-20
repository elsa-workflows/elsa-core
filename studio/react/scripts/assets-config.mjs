import { fileURLToPath } from "node:url";

export const root = fileURLToPath(new URL("../", import.meta.url));
export const aliases = Object.fromEntries(["api-client", "sdk", "auth", "ui"].map(name =>
  [`@elsa-workflows/react-studio-${name}`, fileURLToPath(new URL(`../packages/${name}/src`, import.meta.url))]));

// Public packages and third-party instances are supplied once by the host import map.
// CSS stays with the feature that owns it.
export function externalBare(id) {
  return !id.startsWith(".") && !id.startsWith("/") && !id.startsWith("@/") && !id.startsWith("\0") && !id.startsWith("vite/") && !id.endsWith(".css");
}

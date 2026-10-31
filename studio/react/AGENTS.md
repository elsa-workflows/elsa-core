# React Studio Preview

Read the repository root guidance and quality principles. This is a Vite SPA, with
runtime configuration resolved by the ASP.NET host. Do not add Next.js APIs or load
server environment variables in browser code. Keep the configuration schema and
STUDIO_* compatibility documented in docs/configuration.md.

From this directory use pnpm install --frozen-lockfile, pnpm lint, pnpm typecheck,
pnpm test, and pnpm build. Browser tests exercise the actual ASP.NET host.

The immutable original tree is retained at the import commit recorded in
../../docs/integration-program/studio-react-source-import.json.
Subsequent adaptations must not rewrite donor history or attribution.

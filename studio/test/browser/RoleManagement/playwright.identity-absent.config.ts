import { defineConfig, type Project } from '@playwright/test';

const viewports = [
  { name: '320', width: 320, height: 900 },
  { name: '768', width: 768, height: 900 },
  { name: '1024', width: 1024, height: 900 },
  { name: '1440', width: 1440, height: 900 }
];

const hosts = [
  { name: 'server', url: process.env.ROLE_E2E_SERVER_STUDIO_URL },
  { name: 'wasm', url: process.env.ROLE_E2E_WASM_STUDIO_URL }
].filter(host => host.url) as Array<{ name: string; url: string }>;

const projects: Project[] = hosts.length === 0
  ? [{ name: 'not-configured', use: { baseURL: 'http://127.0.0.1:9', browserName: 'chromium' as const } }]
  : hosts.flatMap(host => viewports.map(viewport => ({
      name: `${host.name}-${viewport.name}`,
      use: {
        baseURL: host.url,
        browserName: 'chromium' as const,
        ignoreHTTPSErrors: true,
        viewport: { width: viewport.width, height: viewport.height }
      }
    })));

export default defineConfig({
  testDir: '.',
  testMatch: 'identity-absent.spec.ts',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  workers: 1,
  retries: 0,
  reporter: 'list',
  use: { serviceWorkers: 'block' },
  projects
});

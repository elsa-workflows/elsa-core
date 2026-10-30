import { defineConfig, type Project } from '@playwright/test';

const viewports = [
  { name: '320', width: 320, height: 900 },
  { name: '768', width: 768, height: 900 },
  { name: '1024', width: 1024, height: 900 },
  { name: '1440', width: 1440, height: 900 }
];

const hosts: Array<{ name: 'server' | 'wasm'; studioUrl?: string; backendUrl?: string }> = [
  {
    name: 'server',
    studioUrl: process.env.ROLE_E2E_SERVER_STUDIO_URL,
    backendUrl: process.env.ROLE_E2E_SERVER_BACKEND_URL
  },
  {
    name: 'wasm',
    studioUrl: process.env.ROLE_E2E_WASM_STUDIO_URL,
    backendUrl: process.env.ROLE_E2E_WASM_BACKEND_URL
  }
].filter(host => host.studioUrl && host.backendUrl) as Array<{ name: 'server' | 'wasm'; studioUrl: string; backendUrl: string }>;

const projects: Project[] = hosts.length === 0
  ? [{
      name: 'not-configured',
      use: {
        baseURL: 'http://127.0.0.1:9',
        browserName: 'chromium' as const,
        ignoreHTTPSErrors: true
      }
    }]
  : hosts.flatMap(host => viewports.map(viewport => ({
      name: `${host.name}-${viewport.name}`,
      use: {
        baseURL: host.studioUrl,
        browserName: 'chromium' as const,
        ignoreHTTPSErrors: true,
        viewport: { width: viewport.width, height: viewport.height }
      }
    })));

export default defineConfig({
  testDir: '.',
  testMatch: 'role-management.spec.ts',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: 'list',
  preserveOutput: 'failures-only',
  use: {
    trace: 'off',
    video: 'off',
    screenshot: 'off',
    serviceWorkers: 'block'
  },
  projects
});

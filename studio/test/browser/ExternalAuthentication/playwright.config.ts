import { defineConfig } from '@playwright/test';

const baseURL = process.env.EXTERNAL_AUTH_BASE_URL || 'http://127.0.0.1:4178';

export default defineConfig({
  testDir: '.',
  testMatch: ['broker-authentication.spec.ts', 'accessibility.spec.ts'],
  timeout: 30_000,
  use: {
    baseURL,
    trace: 'retain-on-failure'
  },
  webServer: process.env.EXTERNAL_AUTH_BASE_URL ? undefined : {
    command: 'node fixture-server.mjs',
    url: baseURL,
    reuseExistingServer: false
  }
});

import { defineConfig } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const studioValue = process.env.ELSA_COMP_PROOF_STUDIO_URL;
const artifactDirectory = process.env.ELSA_COMP_PROOF_ARTIFACT_DIR;
if (!studioValue || !artifactDirectory || !path.isAbsolute(artifactDirectory))
  throw new Error('proof-harness-config-invalid');

const studioUrl = new URL(studioValue);
if (studioUrl.protocol !== 'http:' ||
    !['127.0.0.1', 'localhost', '[::1]'].includes(studioUrl.hostname) ||
    studioUrl.username || studioUrl.password || studioUrl.search || studioUrl.hash)
  throw new Error('proof-harness-url-invalid');

export default defineConfig({
  testDir: path.dirname(fileURLToPath(import.meta.url)),
  testMatch: 'draft-authoring.spec.ts',
  timeout: 150_000,
  expect: { timeout: 8_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['line']],
  preserveOutput: 'never',
  outputDir: artifactDirectory,
  use: {
    baseURL: studioUrl.origin,
    browserName: 'chromium',
    headless: true,
    viewport: { width: 1440, height: 900 },
    serviceWorkers: 'block',
    storageState: { cookies: [], origins: [] },
    trace: 'off',
    screenshot: 'off',
    video: 'off',
    acceptDownloads: false
  }
});

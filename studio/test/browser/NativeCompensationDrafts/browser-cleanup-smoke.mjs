import { chromium } from './StudioDraftProof/node_modules/playwright/index.mjs';
import { chmod, readFile, rename, writeFile } from 'node:fs/promises';

const capturePath = process.argv[2];
const mode = process.argv[3];
if (!capturePath || !['normal', 'hold'].includes(mode))
  throw new Error('smoke-arguments-invalid');

async function snapshot(browser) {
  const session = await browser.newBrowserCDPSession();
  try {
    const result = await session.send('SystemInfo.getProcessInfo');
    if (!Array.isArray(result.processInfo) || !result.processInfo.some(item => item.type === 'browser'))
      throw new Error('smoke-browser-process-not-reported');
    let previous = { schemaVersion: 1, source: 'Chromium-CDP-SystemInfo.getProcessInfo', snapshots: [] };
    try {
      previous = JSON.parse(await readFile(capturePath, 'utf8'));
    } catch (error) {
      if (error.code !== 'ENOENT')
        throw error;
    }
    previous.snapshots.push({
      capturedAtUnixMs: Date.now(),
      ownerPid: process.pid,
      ownerParentPid: process.ppid,
      processes: result.processInfo.map(({ id, type }) => ({ id, type }))
    });
    const temporary = capturePath + '.tmp';
    await writeFile(temporary, JSON.stringify(previous), { mode: 0o600 });
    await rename(temporary, capturePath);
    await chmod(capturePath, 0o600);
  } finally {
    await session.detach();
  }
}

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
const page = await context.newPage();
await page.setContent('<!doctype html><title>local-cleanup-smoke</title><main>cleanup smoke</main>');
await snapshot(browser);
if (mode === 'hold') {
  await new Promise(() => {});
} else {
  await snapshot(browser);
  await context.close();
  await browser.close();
}

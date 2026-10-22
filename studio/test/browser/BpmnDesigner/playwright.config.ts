import { defineConfig } from '@playwright/test';

// CI installs Playwright's Chromium. Elsewhere, BPMN_DESIGNER_CHROMIUM may point at an already installed Chromium.
const executablePath = process.env.BPMN_DESIGNER_CHROMIUM || undefined;

export default defineConfig({
    testDir: '.',
    testMatch: '*.spec.ts',
    // bpmn-js.spec.ts checks what the bpmn-js designer draws, which the contract leaves to each designer.
    testIgnore: (process.env.BPMN_DESIGNER_MODULE ?? 'bpmn-js-designer.js') === 'bpmn-js-designer.js' ? [] : ['bpmn-js.spec.ts'],
    timeout: 30_000,
    expect: { timeout: 5_000 },
    fullyParallel: true,
    retries: 0,
    reporter: 'list',
    use: {
        browserName: 'chromium',
        viewport: { width: 1280, height: 800 },
        launchOptions: { executablePath },
        trace: 'off',
        video: 'off',
        screenshot: 'off'
    }
});

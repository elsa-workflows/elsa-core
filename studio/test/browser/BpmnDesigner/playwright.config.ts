import { defineConfig } from '@playwright/test';

const designerModule = process.env.BPMN_DESIGNER_MODULE ?? 'bpmn-js-designer.js';
/** The spec that checks what each designer draws, by its module. */
const DESIGNER_SPECS: Record<string, string> = { 'bpmn-js-designer.js': 'bpmn-js.spec.ts', 'x6-designer.js': 'x6.spec.ts' };

// CI installs Playwright's Chromium. Elsewhere, BPMN_DESIGNER_CHROMIUM may point at an already installed Chromium.
const executablePath = process.env.BPMN_DESIGNER_CHROMIUM || undefined;

export default defineConfig({
    testDir: '.',
    testMatch: '*.spec.ts',
    // bpmn-js.spec.ts and x6.spec.ts check what one designer draws, which the contract leaves to each designer.
    testIgnore: Object.entries(DESIGNER_SPECS).filter(([module]) => module !== designerModule).map(([, spec]) => spec),
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

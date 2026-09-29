import { defineConfig } from 'vitest/config';

// Two suites:
//
//  - `*.test.ts`, run in a jsdom environment. src/bpmn/di-reader.ts parses BPMN DI with the platform
//    `DOMParser`, which is what the browser gives it at runtime; jsdom is the one test environment
//    that provides the same API with real XML namespace support, so the reader under test is the
//    reader that ships rather than an injected stand-in.
//  - `*.test-d.ts`, run by Vitest's typecheck mode. Whether src/bpmn/types.generated.ts still
//    describes the JSON Elsa's activity serializer produces is a compile-time question, so it is
//    asked with a type-level assertion rather than a runtime one -- see
//    src/bpmn/__tests__/process-payload.test-d.ts.
//
// Covered: src/bpmn, and the pure half of the X6 BPMN adapter under src/designer/bpmn -- its cell
// mapping, badge policy and graph options are all plain functions precisely so that they can be
// tested here rather than only in a browser. The rest of the designer and react-designer bundles
// have no test suite; they are covered by the .NET side.
export default defineConfig({
    test: {
        environment: 'jsdom',
        include: ['src/**/*.test.ts'],
        typecheck: {
            enabled: true,
            include: ['src/**/*.test-d.ts'],
        },
    },
});

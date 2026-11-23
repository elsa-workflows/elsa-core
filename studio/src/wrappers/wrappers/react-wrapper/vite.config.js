import { defineConfig } from 'vite'
import { writeFileSync } from 'node:fs'
import { resolve } from 'path'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react(),
    process.env.ELSA_BUNDLE_INPUTS && {
      name: 'record-packed-module-inputs',
      generateBundle(_, bundle) {
        for (const chunk of Object.values(bundle)) {
          if (chunk.type !== 'chunk') continue
          const inputs = Object.entries(chunk.modules)
            .filter(([, module]) => module.renderedLength > 0)
            .map(([path]) => path)
          writeFileSync(resolve(process.env.ELSA_BUNDLE_INPUTS, chunk.fileName + '.json'), JSON.stringify(inputs))
        }
      },
    },
  ],
  build: {
    lib: {
      // Could also be a dictionary or array of multiple entry points
      entry: resolve(__dirname, 'src/main.js'),
      name: 'Elsa Studio WASM React Wrapper',
      // the proper extensions will be added
      fileName: 'elsa-studio-wasm-react',
    },
    rollupOptions: {
      // make sure to externalize deps that shouldn't be bundled
      // into your library
      external: ['react'],
    },
  },
})

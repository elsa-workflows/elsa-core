import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import cp from 'vite-plugin-cp';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    cp({
      targets: [
        { 
          src: '../../elsa-workflows-designer/dist/elsa-workflows-designer/assets/logo.png', 
          dest: './public/assets',
        }
      ]
    }),
    react()],
})

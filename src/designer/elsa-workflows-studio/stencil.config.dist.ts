import { Config } from '@stencil/core';
import { postcss } from '@stencil/postcss';
import postcssImport from 'postcss-import';
import tailwindcss from 'tailwindcss';
import cssnano from 'cssnano';
import replace from '@rollup/plugin-replace';

const enableX6 = (process.argv && process.argv.indexOf('--x6graph') > -1) ? "'true'" : "'false'";

// @ts-ignore
const purgecss = require('@fullhuman/postcss-purgecss')({
  content: ['./src/**/*.tsx', './src/**/*.html'],
  defaultExtractor: content => content.match(/[A-Za-z0-9-_:/]+/g) || [],
  safelist: ['jtk-connector', 'jtk-endpoint', 'x6-node', 'x6-port-out', 'x6-port-label', 'x6-graph-scroller'],
});

export const config: Config = {
  namespace: 'elsa-workflows-studio',
  outputTargets: [
    {
      type: 'dist',
      esmLoaderPath: '../loader',
      copy: [{ src: 'assets', dest: 'assets' }],
    },
    {
      type: 'dist-custom-elements-bundle',
    },
    {
      type: 'docs-readme',
    },
  ],
  globalStyle: 'src/globals/tailwind.css',
  plugins: [
    replace({ 'process.env.ENABLE_X6_WORKFLOW': enableX6 }),
    postcss({
      plugins: [postcssImport, tailwindcss, purgecss, cssnano],
    }),
  ],
};

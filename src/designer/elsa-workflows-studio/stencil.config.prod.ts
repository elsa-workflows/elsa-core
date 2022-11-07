import {Config} from '@stencil/core';
import {postcss} from '@stencil/postcss';
import postcssImport from 'postcss-import';
import tailwindcss from 'tailwindcss';
import cssnano from 'cssnano';
import replace from '@rollup/plugin-replace';

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
      type: 'www',
      serviceWorker: null, // disable service workers,
      copy: [
        {src: 'assets', dest: 'build/assets'},
        {src: '../node_modules/monaco-editor/min', dest: 'build/assets/js/monaco-editor/min'},
        {src: '../node_modules/monaco-editor/min-maps', dest: 'build/assets/js/monaco-editor/min-maps'}
      ]
    },
  ],
  globalStyle: 'src/globals/tailwind.css',
  plugins: [
    postcss({
      plugins: [
        postcssImport,
        tailwindcss,
        purgecss,
        cssnano
      ]
    })
  ],
};

import {Config} from '@stencil/core';
import {postcss} from '@stencil/postcss';
import postcssImport from 'postcss-import';
import tailwindcss from 'tailwindcss';
import cssnano from 'cssnano';

// @ts-ignore
const dev: boolean = process.argv && process.argv.indexOf('--dev') > -1;

// @ts-ignore
const tailwindDev: boolean = process.argv && process.argv.indexOf('--tailwind:dev') > -1;

// @ts-ignore
const purgecss = require('@fullhuman/postcss-purgecss')({
  content: ['./src/**/*.tsx', './src/**/*.html'],
  defaultExtractor: content => content.match(/[A-Za-z0-9-_:/]+/g) || [],
  safelist: ['hidden', 'jtk-connector', 'jtk-endpoint', 'x6-node', 'x6-node-selected', 'elsa-default-activity-template', 'x6-port-out', 'x6-port-label', 'x6-graph-scroller', 'rose', 'sky', /gray/, /pink/, /blue/, /green/, /red/, /yellow/, /rose/, 'label-container', 'node', 'start', 'activity', 'elsa-border-blue-600', 'elsa-border-green-600', 'elsa-border-red-600'],
});

export const config: Config = {
  namespace: 'elsa-workflows-studio',
  outputTargets: [
    {
      type: 'dist',
      esmLoaderPath: '../loader',
      copy: [
        {src: 'assets', dest: 'assets'}
      ]
    },
    {
      type: 'dist-custom-elements-bundle',
    },
    {
      type: 'docs-readme',
    },
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
  plugins: tailwindDev
    ? []
    : [postcss({
        plugins: [
          postcssImport,
          tailwindcss,
          purgecss,
          cssnano
        ]
      })
    ],
};

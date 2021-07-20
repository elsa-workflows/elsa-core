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
const dashboardBuildOnly: boolean = process.argv && process.argv.indexOf('--dasboard') > -1;

// @ts-ignore
const purgecss = require('@fullhuman/postcss-purgecss')({
  content: ['./src/**/*.tsx', './src/**/*.html'],
  defaultExtractor: content => content.match(/[A-Za-z0-9-_:/]+/g) || [],
  safelist: ['jtk-connector'],
});

const targets = [];

targets.push({
  type: 'dist',
  esmLoaderPath: '../loader',
  copy: [
    { src: 'assets', dest: 'assets' }
  ]
});

targets.push({
  type: 'dist-custom-elements-bundle',
});

if (!dashboardBuildOnly) {
  targets.push({
    type: 'docs-readme'
  });

  targets.push({
    type: 'www',
    serviceWorker: null, // disable service workers,
    copy: [
      { src: 'assets', dest: 'build/assets' },
      { src: '../node_modules/monaco-editor/min', dest: 'build/assets/js/monaco-editor/min' },
      { src: '../node_modules/monaco-editor/min-maps', dest: 'build/assets/js/monaco-editor/min-maps' }
    ]
  });
}

export const config: Config = {
  namespace: 'elsa-workflows-studio',
  outputTargets: targets,
  globalStyle: 'src/globals/tailwind.css',
  plugins: tailwindDev
    ? []
    : [
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

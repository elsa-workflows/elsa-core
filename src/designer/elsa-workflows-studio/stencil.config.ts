import {Config} from '@stencil/core';
import {postcss} from '@stencil/postcss';
import postcssImport from 'postcss-import';
import tailwindcss from 'tailwindcss';
import cssnano from 'cssnano';

const dev: boolean = process.argv && process.argv.indexOf('--dev') > -1;

// @ts-ignore
const purgecss = require('@fullhuman/postcss-purgecss')({
  content: ['./src/**/*.tsx', './src/**/*.css', './src/**/*.html'],
  defaultExtractor: content => content.match(/[A-Za-z0-9-_:/]+/g) || [],
  safelist: ['jtk-connector'],
});

export const config: Config = {
  namespace: 'elsa-workflows-studio',
  outputTargets: [
    {
      type: 'dist',
      esmLoaderPath: '../loader',
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
        {src: 'assets', dest: 'assets'}
      ]
    },
  ],
  globalStyle: 'src/globals/styles.css',
  plugins: dev ? [] : [
    postcss({
      plugins: dev
        ? [postcssImport,
          tailwindcss]
        : [postcssImport,
          tailwindcss,
          purgecss,
          cssnano
        ],
      injectGlobalPaths: [
        'src/globals/tailwind.css'
      ]
    })
  ]
};

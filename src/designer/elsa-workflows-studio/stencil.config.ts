import {Config} from '@stencil/core';
import {postcss} from '@stencil/postcss';
import postcssImport from 'postcss-import';
import tailwindcss from 'tailwindcss';
import cssnano from 'cssnano';

const purgecss = require('@fullhuman/postcss-purgecss')({
  content: ['./src/**/*.tsx', './src/index.html', './src/**/*.pcss'],
  defaultExtractor: content => content.match(/[A-Za-z0-9-_:/]+/g) || []
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
        {src: 'assets/inter', dest: 'assets/inter'}
      ]
    },
  ],
  globalStyle: 'src/globals/styles.css',
  plugins: [
    postcss({
      plugins: [
        postcssImport,
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

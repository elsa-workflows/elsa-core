import {Config} from '@stencil/core';
import {postcss} from '@stencil/postcss';
import {sass} from '@stencil/sass';
import autoprefixer from 'autoprefixer';
import tailwindcss from 'tailwindcss';
import purgecss from '@fullhuman/postcss-purgecss';

// @ts-ignore
const dev: boolean = process.argv && process.argv.indexOf('--dev') > -1;

export const config: Config = {
  namespace: 'elsa-workflows-designer',
  globalStyle: 'src/global/tailwind.css',
  outputTargets: [
    {
      type: 'dist',
      esmLoaderPath: '../loader',
    },
    {
      type: 'dist-custom-elements-bundle',
    },
    {
      type: 'www',
      serviceWorker: null, // disable service workers
    },
  ],
  plugins: [
    sass(),
    postcss({
      plugins: [
        tailwindcss({}),
        autoprefixer({}),
        ...(dev ? [] : [purgecss({content: ['./**/*.tsx', './**/*.ts']})])
      ]
    })
  ]
};

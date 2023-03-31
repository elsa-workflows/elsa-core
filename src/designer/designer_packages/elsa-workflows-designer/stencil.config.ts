import {Config} from '@stencil/core';
import {postcss} from '@stencil/postcss';
import {sass} from '@stencil/sass';
import autoprefixer from 'autoprefixer';
import tailwindcss from 'tailwindcss';
import purgecss from '@fullhuman/postcss-purgecss';

import { angularOutputTarget } from '@stencil/angular-output-target';
import { reactOutputTarget } from '@stencil/react-output-target';

// @ts-ignore
const dev: boolean = process.argv && process.argv.indexOf('--dev') > -1;

export const config: Config = {
  namespace: 'elsa-workflows-designer',
  globalStyle: 'src/global/tailwind.scss',
  outputTargets: [
    {
      type: 'dist',
      esmLoaderPath: '../loader',
    },
    {
      type: 'dist-custom-elements',
    },
    angularOutputTarget({
      componentCorePackage: '@elsa-workflows/elsa-workflows-designer',
      directivesProxyFile: '../angular-workspace/projects/component-library/src/lib/stencil-generated/components.ts',
      directivesArrayFile: '../angular-workspace/projects/component-library/src/lib/stencil-generated/index.ts',
      excludeComponents:['context-consumer']
    }),
    reactOutputTarget({
      componentCorePackage: '@elsa-workflows/elsa-workflows-designer',
      proxiesFile: '../react-wrapper/lib/components/stencil-generated/index.ts',
      excludeComponents:['context-consumer']
    }),
    {
      type: 'www',
      serviceWorker: null, // disable service workers
    },
  ],
  plugins: [
    sass(),
    postcss({
      plugins: [
        tailwindcss(),
        autoprefixer(),
        ...(dev ? [] : [purgecss({
          content: ['./**/*.tsx', './**/*.ts'],
          defaultExtractor: (content) => content.match(/[\w-/:.]+(?<!:)/g) || [],
        })])
      ]
    })
  ]
};

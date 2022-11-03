import {Config} from '@stencil/core';
import replace from '@rollup/plugin-replace';

const enableX6 = (process.argv && process.argv.indexOf('--x6graph') > -1) ? "'true'" : "'false'";

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
  plugins: [replace({'process.env.ENABLE_X6_WORKFLOW': enableX6})]
};

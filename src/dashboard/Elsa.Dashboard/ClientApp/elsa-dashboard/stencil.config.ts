import { Config } from '@stencil/core';
import { sass } from "@stencil/sass";
import nodePolyfills from 'rollup-plugin-node-polyfills';

export const config: Config = {
  globalStyle: 'src/global/app.css',
  globalScript: 'src/global/app.ts',
  devServer:{
    initialLoadUrl: '/elsa-dashboard',
    port: 4012
  },
  outputTargets: [
    {
      type: 'www',
      // comment the following line to disable service workers in production
      serviceWorker: null,
      dir: '../../wwwroot'
    }
  ],
  plugins: [
    sass(),
    nodePolyfills()
  ],
  copy: [
    { src: '../node_modules/sleek-dashboard/dist/assets/css', dest: 'assets/css'},
    { src: '../node_modules/sleek-dashboard/dist/assets/js', dest: 'assets/js'},
    { src: '../node_modules/sleek-dashboard/dist/assets/plugins', dest: 'assets/plugins'},
    { src: '../node_modules/sleek-dashboard/dist/assets/img', dest: 'assets/img'},
  ]
};

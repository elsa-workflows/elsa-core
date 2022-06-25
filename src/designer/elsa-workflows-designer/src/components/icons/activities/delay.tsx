import {FunctionalComponent, h} from '@stencil/core';
import {getActivityIconCssClass, ActivityIconSettings} from "./models";

export const DelayIcon: FunctionalComponent<ActivityIconSettings> = (settings) => (
  <svg class={getActivityIconCssClass(settings)} width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <path d="M9 4.55a8 8 0 0 1 6 14.9m0 -4.45v5h5"/>
    <path d="M11 19.95a8 8 0 0 1 -5.3 -12.8" stroke-dasharray=".001 4.13"/>
  </svg>
);

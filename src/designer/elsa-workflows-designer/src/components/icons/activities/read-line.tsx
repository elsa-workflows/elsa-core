import {FunctionalComponent, h} from '@stencil/core';
import {ActivityIconSettings, getActivityIconCssClass} from "./models";

export const ReadLineIcon: FunctionalComponent<ActivityIconSettings> = (settings) => (
  <svg class={getActivityIconCssClass(settings)} width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <path d="M5 5h3m4 0h9"/>
    <path d="M3 10h11m4 0h1"/>
    <path d="M5 15h5m4 0h7"/>
    <path d="M3 20h9m4 0h3"/>
  </svg>
);

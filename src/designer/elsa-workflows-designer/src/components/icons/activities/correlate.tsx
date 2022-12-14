import {FunctionalComponent, h} from '@stencil/core';
import {ActivityIconSettings, getActivityIconCssClass} from "./models";

export const CorrelateIcon: FunctionalComponent<ActivityIconSettings> = (settings) => (
<svg class={getActivityIconCssClass(settings)} width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"
     fill="none" stroke-linecap="round" stroke-linejoin="round">
  <path stroke="none" d="M0 0h24v24H0z"/>
  <circle cx="6" cy="6" r="2"/>
  <circle cx="18" cy="18" r="2"/>
  <path d="M11 6h5a2 2 0 0 1 2 2v8"/>
  <polyline points="14 9 11 6 14 3"/>
  <path d="M13 18h-5a2 2 0 0 1 -2 -2v-8"/>
  <polyline points="10 15 13 18 10 21"/>
</svg>
);

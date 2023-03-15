import {FunctionalComponent, h} from '@stencil/core';
import {ActivityIconSettings, getActivityIconCssClass} from "./models";

export const RunJavaScriptIcon: FunctionalComponent<ActivityIconSettings> = (settings) => (
  <svg class={getActivityIconCssClass(settings)} width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <path d="M14 3v4a1 1 0 0 0 1 1h4"/>
    <path d="M17 21h-10a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2h7l5 5v11a2 2 0 0 1 -2 2z"/>
    <path d="M10 13l-1 2l1 2"/>
    <path d="M14 13l1 2l-1 2"/>
  </svg>
);

import {h} from '@stencil/core';
import {Map} from '../utils/utils';

export enum IconName {
  Plus = 'plus',
  TrashBinOutline = 'trash-bin-outline',
  OpenInDialog = 'open-in-dialog'
}

export enum IconColor {
  Blue = 'blue',
  Gray = 'gray',
  Green = 'green',
  Red = 'red',
  Default = 'currentColor'
}

export interface IconProviderOptions {
  color?: IconColor,
  hoverColor?: IconColor
}

export class IconProvider {
  private map: Map<(options?: IconProviderOptions) => any> = {
    'plus': (options?: IconProviderOptions) =>
      <svg
        class={`-elsa-ml-1 elsa-mr-2 elsa-h-5 elsa-w-5 ${options?.color ? `elsa-text-${options.color}-500` : ''} ${options?.hoverColor ? `hover:elsa-text-${options.hoverColor}-500` : ''}`}
        width="24" height="24" viewBox="0 0 24 24"
        stroke-width="2" stroke="currentColor" fill="transparent" stroke-linecap="round"
        stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <line x1="12" y1="5" x2="12" y2="19"/>
        <line x1="5" y1="12" x2="19" y2="12"/>
      </svg>,
    'trash-bin-outline': (options?: IconProviderOptions) =>
      <svg
        class={`elsa-h-5 elsa-w-5 ${options?.color ? `elsa-text-${options.color}-500` : ''} ${options?.hoverColor ? `hover:elsa-text-${options.hoverColor}-500` : ''}`}
        width="24" height="24" viewBox="0 0 24 24"
        stroke-width="2" stroke="currentColor" fill="transparent" stroke-linecap="round"
        stroke-linejoin="round">
        <polyline points="3 6 5 6 21 6"/>
        <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
        <line x1="10" y1="11" x2="10" y2="17"/>
        <line x1="14" y1="11" x2="14" y2="17"/>
      </svg>,
    'open-in-dialog': (options?: IconProviderOptions) =>
      <svg xmlns="http://www.w3.org/2000/svg" height="48" width="48"
        class={`${options?.color ? `elsa-text-${options.color}-500` : ''} ${options?.hoverColor ? `hover:elsa-text-${options.hoverColor}-500` : ''}`}>
        <path style={{ transform: "scale(0.5)" }} d="M9 42q-1.2 0-2.1-.9Q6 40.2 6 39V9q0-1.2.9-2.1Q7.8 6 9 6h13.95v3H9v30h30V25.05h3V39q0 1.2-.9 2.1-.9.9-2.1.9Zm10.1-10.95L17 28.9 36.9 9H25.95V6H42v16.05h-3v-10.9Z" />
      </svg>
  };

  getIcon(name: IconName, options?: IconProviderOptions): any {
    const provider = this.map[name];

    if (!provider)
      return undefined;

    return provider(options);
  }
}

export const iconProvider = new IconProvider();

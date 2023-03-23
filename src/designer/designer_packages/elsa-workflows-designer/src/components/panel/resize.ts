import { PanelPosition } from './models';

type ApplyResizeParams = {
  position: PanelPosition;
  isDefault?: boolean;
  isHide?: boolean;
  size?: number;
};

const positionToLocalStorage = {
  [PanelPosition.Right]: 'LS/rightPanelWidth',
  [PanelPosition.Left]: 'LS/leftPanelWidth',
  [PanelPosition.Bottom]: 'LS/bottomPanelWidth',
};

const positionToCssVariable = {
  [PanelPosition.Right]: '--workflow-editor-width',
  [PanelPosition.Left]: '--activity-picker-width',
  [PanelPosition.Bottom]: '--activity-editor-height',
};

const positionToDefaultSize = {
  [PanelPosition.Right]: 580,
  [PanelPosition.Left]: 300,
  [PanelPosition.Bottom]: 200,
};

function getPanelDefaultSize(position: PanelPosition) {
  return localStorage.getItem(positionToLocalStorage[position]) || positionToDefaultSize[position];
}

function updatePanelDefaultSize(position: PanelPosition, size: number) {
  return localStorage.setItem(positionToLocalStorage[position], size.toString());
}

export function applyResize({ position, isDefault, isHide, size }: ApplyResizeParams) {
  const root = document.querySelector<HTMLElement>(':root');

  if (isHide) {
    root.style.setProperty(positionToCssVariable[position], '1px');
    return;
  }

  if (isDefault) {
    root.style.setProperty(positionToCssVariable[position], `${getPanelDefaultSize(position)}px`);
    return;
  }

  root.style.setProperty(positionToCssVariable[position], `${size}px`);
  updatePanelDefaultSize(position, size);
  return;
}

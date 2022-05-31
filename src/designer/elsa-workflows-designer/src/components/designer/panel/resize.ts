import { PanelPosition } from './models';

type ApplyResizeParams = {
  position: PanelPosition;
  isDefault?: boolean;
  isHide?: boolean;
  width?: number;
};

const positionToLocalStorage = {
  [PanelPosition.Right]: 'LS/rightPanelWidth',
  [PanelPosition.Left]: 'LS/leftPanelWidth',
};

const positionToCssVariable = {
  [PanelPosition.Right]: '--activity-editor-width',
  [PanelPosition.Left]: '--activity-picker-width',
};

const positionToDefaultWidth = {
  [PanelPosition.Right]: 580,
  [PanelPosition.Left]: 300,
};

function getPanelDefaultWidth(position: PanelPosition) {
  return localStorage.getItem(positionToLocalStorage[position]) || positionToDefaultWidth[position];
}

function updatePanelDefaultWidth(position: PanelPosition, width: number) {
  return localStorage.setItem(positionToLocalStorage[position], width.toString());
}

export function applyResize({ position, isDefault, isHide, width }: ApplyResizeParams) {
  const root = document.querySelector<HTMLElement>(':root');

  if (isHide) {
    root.style.setProperty(positionToCssVariable[position], '0');
    return;
  }

  if (isDefault) {
    root.style.setProperty(positionToCssVariable[position], `${getPanelDefaultWidth(position)}px`);
    return;
  }

  root.style.setProperty(positionToCssVariable[position], `${width}px`);
  updatePanelDefaultWidth(position, width);
  return;
}

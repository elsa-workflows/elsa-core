export interface ContextMenuItem {
  text: string;
  anchorUrl?: string;
  handler?: (e: MouseEvent) => void;
  icon?: any;
  isToggle?: boolean; // Whether this item can be checked/unchecked.
  checked?: boolean;
  order?: number;
  group?: string;
}

export enum ContextMenuAnchorPoint {
  TopLeft,
  TopRight,
  BottomLeft,
  BottomRight
}

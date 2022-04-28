export interface MenuItem {
  text: string;
  anchorUrl?: string;
  clickHandler?: (e: MouseEvent) => void;
  icon?: any;
  isToggle?: boolean; // Whether this item can be checked/unchecked.
  checked?: boolean;
}

export interface MenuItemGroup {
  menuItems: Array<MenuItem>
}

export enum ContextMenuAnchorPoint {
  TopLeft,
  TopRight,
  BottomLeft,
  BottomRight
}

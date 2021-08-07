export enum DropdownButtonOrigin
{
  TopLeft,
  TopRight
}

export interface DropdownButtonItem {
  name?: string;
  value?: any;
  text: string;
  url?: string;
  isSelected?: boolean;
  handler?: () => {};
}

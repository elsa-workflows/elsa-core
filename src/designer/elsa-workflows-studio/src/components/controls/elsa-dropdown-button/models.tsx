export enum DropdownButtonOrigin
{
  TopLeft,
  TopRight
}

export interface DropdownButtonItem {
  name: string;
  text: string;
  url?: string;
  isSelected?: boolean;
}

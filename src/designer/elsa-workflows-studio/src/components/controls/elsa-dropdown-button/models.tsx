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
  btnClass? : string
  isSelected?: boolean;
  handler?: () => {};
}

export interface MenuItem {
  text: string;
  anchorUrl?: string;
  clickHandler?: (e: MouseEvent) => void;
  icon?: any;
}

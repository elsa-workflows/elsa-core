export interface MenuItem {
  text: string;
  anchorUrl?: string;
  clickHandler?: (e: Event) => void;
  icon?: any;
}
